// Generuje hasz o długości 64 bitów z obrazka odcisku palca. Zwraca go jako 16-znakowy string heksadecymalny.
public static string CalculateAverageHash(byte[] imageBytes)
{
    try
    {
        using (var ms = new MemoryStream(imageBytes))
        {
            using (var originalBitmap = new Bitmap(ms))
            {
                // Zmień rozmiar do 8x8 pikseli w celu znormalizowania
                using (var resizedBitmap = new Bitmap(originalBitmap, new Size(8, 8)))
                {
                    int[] grayscales = new int[64];
                    int sum = 0;

                    // Konwersja na odcienie szarości i wyliczenie sumy jasności
                    int index = 0;
                    for (int y = 0; y < 8; y++)
                    {
                        for (int x = 0; x < 8; x++)
                        {
                            Color pixel = resizedBitmap.GetPixel(x, y);
                            // Wyliczenie jasności metodą luminancji (BT.601)
                            int gray = (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);
                            grayscales[index] = gray;
                            sum += gray;
                            index++;
                        }
                    }

                    // Obliczenie średniej wartości jasności piksela
                    int average = sum / 64;

                    // Generowanie haszu - ustawienie bitu na 1, jeśli jasność >= średniej
                    ulong hash = 0;
                    for (int i = 0; i < 64; i++)
                    {
                        if (grayscales[i] >= average)
                        {
                            hash |= (1UL << i);
                        }
                    }

                    return hash.ToString("X16");
                }
            }
        }
    }
    catch
    {
        return string.Empty;
    }
}