private void HandleFingerprintProcessing(byte[] imgBytes)
{
    FingerprintPreview = imgBytes;

    // Wyliczenie haszu próbki wejściowej
    string inputHash = FingerprintHashing.CalculateAverageHash(imgBytes);

    if (string.IsNullOrEmpty(inputHash))
    {
        CustomMessageBox.Show("Nie można odczytać próbki linii papilarnych z pliku.", "Błąd odczytu", MessageBoxButton.OK, MessageBoxImage.Error);
        ResumeScanning();
        return;
    }

    try
    {
        using (var context = new RockGymContext())
        {
            // Pobierz wszystkie zarejestrowane odciski z bazy
            var dbFingerprints = context.Fingerprints
                .Select(f => new { f.UserId, f.FingerprintHash })
                .ToList();

            if (!dbFingerprints.Any())
            {
                CustomMessageBox.Show("Brak jakichkolwiek zarejestrowanych wzorców odcisków palców w bazie danych.", "Baza pusta", MessageBoxButton.OK, MessageBoxImage.Information);
                ResumeScanning();
                return;
            }

            ulong bestMatchUserId = 0;
            double bestMatchProbability = 0.0;

            // Porównanie z każdym wzorcem z bazy
            foreach (var fp in dbFingerprints)
            {
                double probability = FingerprintHashing.CalculateMatchProbability(inputHash, fp.FingerprintHash);
                if (probability > bestMatchProbability)
                {
                    bestMatchProbability = probability;
                    bestMatchUserId = fp.UserId;
                }
            }

            MatchProbabilityText = $"Dopasowanie: {bestMatchProbability:0.00}%";

            // Jeśli dopasowanie jest poniżej progu 82% - odrzucamy
            if (bestMatchProbability < 82.0 || bestMatchUserId == 0)
            {
                MatchProbabilityColor = new SolidColorBrush(Color.FromRgb(229, 62, 62));
                CustomMessageBox.Show($"Nie odnaleziono pasującego użytkownika w bazie danych.\nNajlepsze dopasowanie: {bestMatchProbability:0.00}%.", "Brak dopasowania", MessageBoxButton.OK, MessageBoxImage.Warning);
                
                EmptyCardVisibility = Visibility.Visible;
                ClientCardVisibility = Visibility.Collapsed;
                ResumeButtonVisibility = Visibility.Visible;
                return;
            }

            // Sukces
            MatchProbabilityColor = new SolidColorBrush(Color.FromRgb(72, 187, 120));
            _currentUserId = bestMatchUserId;

            // Pobranie danych pasującego klienta
            var user = context.Users
                .Include(u => u.Role)
                .Include(u => u.Entrances)
                .Include(u => u.CustomerPurchases)
                    .ThenInclude(p => p.Offer)
                .FirstOrDefault(u => u.UserId == bestMatchUserId);

            if (user == null)
            {
                CustomMessageBox.Show("Pasujący użytkownik nie istnieje w bazie danych.", "Błąd spójności", MessageBoxButton.OK, MessageBoxImage.Error);
                ResumeScanning();
                return;
            }

            ClientName = user.FullName;
            ClientRole = user.Role?.Name ?? "klient";
            StyleRoleBadge(user.RoleId);

            bool isActive = user.Entrances.Any(e =>
                e.DateOfEntry.Date == DateTime.Today &&
                e.StartTime <= DateTime.Now.TimeOfDay &&
                e.EndTime == null);

            if (isActive)
            {
                ClientStatusBrush = new SolidColorBrush(Color.FromRgb(72, 187, 120));
                ClientStatusText = "Aktywny";
            }
            else
            {
                ClientStatusBrush = new SolidColorBrush(Color.FromRgb(160, 174, 192));
                ClientStatusText = "Nieaktywny";
            }

            ProfilePicture = user.ProfilePicture;

            // Pobranie aktywnych karnetów
            ActiveOffers.Clear();
            var activePurchases = user.CustomerPurchases
                .Where(p => p.Offer != null && p.Offer.Duration != 0 && p.PurchaseDate.AddDays(p.Offer.Duration) >= DateTime.Now)
                .ToList();

            foreach (var purchase in activePurchases)
            {
                DateTime expirationDate = purchase.PurchaseDate.AddDays(purchase.Offer?.Duration ?? 0);
                ActiveOffers.Add(new ActiveOfferItemViewModel
                {
                    Name = purchase.Offer?.Name ?? "Oferta",
                    ValidityText = $"Ważny od: {purchase.PurchaseDate:dd.MM.yyyy} do: {expirationDate:dd.MM.yyyy}"
                });
            }

            EmptyCardVisibility = Visibility.Collapsed;
            ClientCardVisibility = Visibility.Visible;
            ResumeButtonVisibility = Visibility.Visible;
        }
    }
    catch (Exception ex)
    {
        CustomMessageBox.Show($"Błąd podczas przetwarzania odcisku: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        ResumeScanning();
    }
}