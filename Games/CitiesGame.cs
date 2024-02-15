using Microsoft.AspNetCore.SignalR;
using SpeechWebGame.Tools;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace SpeechWebGame.Games
{
    [SupportedOSPlatform("windows")]
    public class CitiesGame : ISpeechGame
    {
        private readonly SpeechService _speechService;
        private readonly InputService _inputService;

        public CitiesGame(
            SpeechService speechService,
            InputService inputService)
        {
            _speechService = speechService;
            _inputService = inputService;
        }

        public static GameInfo GameInfo { get; } = new()
        {
            Id = "cities",
            MinPlayers = 1,
            MaxPlayers = 1,
            Title = "Нужно назвать город России"
        };

        private record CityRecord(string FederalDistrict, string RegionType, string Region, string AreaType, string Area, string CityType, string City, int Population, int FoundationYear);

        public async Task RunAsync(ISingleClientProxy client, CancellationToken token)
        {
            var lines = File.ReadLinesAsync("Data\\city.csv", token);

            var cities = new List<CityRecord>(1000);

            var csvParserRegex = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))", RegexOptions.Compiled);

            bool header = true;
            await foreach (var line in lines.WithCancellation(token))
            {
                if (header)
                {
                    header = false;
                    continue;
                }
                var items = csvParserRegex.Split(line);

                var regionType = items[4] switch
                {
                    "Респ" => "Республика",
                    _ => items[4]
                };

                var city = new CityRecord(items[3], regionType, items[5], items[6], items[7], items[8], items[9], int.Parse(items[22]), int.Parse(items[23]));
                cities.Add(city);
            }

            cities = cities.OrderByDescending(q => q.Population).ToList();

            var cityNames = cities.Select(q => new[] { q.City, q.City.ToLower().Replace('ё', 'е').Replace('-', ' ') }).ToArray();
            var usedCitySet = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            var rand = new Random();
            var playerTurn = rand.NextSingle() > 0.5f;
            if (playerTurn)
            {
                await _speechService.PlaySpeechAsync(client, "Начинаем. Ваш ход. Назовите город.", cancellationToken: token);
            }
            else
            {
                await _speechService.PlaySpeechAsync(client, "Начинаем. Мой ход.", cancellationToken: token);
            }

            char[] startChars = Array.Empty<char>();

            string? FindCityForWin()
            {
                return cityNames.Select(q => q[0]).Except(usedCitySet).OrderBy(q => rand.Next())
                    .FirstOrDefault(q => startChars.Length == 0 || startChars.Any(q.StartsWith));
            }

            while (true)
            {
                if (playerTurn)
                {
                    string? foundCity = null;
                    bool TestFunc(string s, TimeSpan t)
                    {
                        s = s.Replace('ё', 'е');
                        var city = cityNames.FirstOrDefault(q => q.Any(w => w.Equals(s, StringComparison.CurrentCultureIgnoreCase)));
                        if (city == null)
                        {
                            city = cityNames.FirstOrDefault(q => q.Any(w =>
                                s.Length >= w.Length
                                && s.Contains(w, StringComparison.CurrentCultureIgnoreCase)));
                        }

                        if (city != null)
                        {
                            foundCity = city[0];
                            return true;
                        }

                        if (t.TotalSeconds > 10 && s.Length >= 3)
                        {
                            return true;
                        }

                        return false;
                    }

                    var result = await _inputService.GetInputAsync(client, true, TimeSpan.FromMinutes(1), TestFunc, token, variants: new[] { FindCityForWin() }.Where(q => q != null).ToArray()!);
                    if (result.Contains("каспийска"))
                        result = result.Append("каспийск").ToArray();
                    if (foundCity == null)
                    {
                        foreach (var s in result)
                        {
                            if (TestFunc(s, default))
                                break;
                        }
                    }

                    if (string.IsNullOrEmpty(foundCity))
                    {
                        var ww = result.FirstOrDefault(q => q.Length > 0);
                        if (ww != null)
                        {
                            await _speechService.PlaySpeechAsync(client, $"{ww}? Такого города в России нет.", cancellationToken: token);
                        }
                        await _speechService.PlaySpeechAsync(client, "Назовите город", cancellationToken: token);
                        continue;
                    }

                    if (!usedCitySet.Add(foundCity))
                    {
                        await _speechService.PlaySpeechAsync(client, $"{foundCity}? Такой город уже назывался ранее.", cancellationToken: token);
                        await _speechService.PlaySpeechAsync(client, "Назовите город", cancellationToken: token);
                        continue;
                    }

                    startChars = GetLastCharsFromCity(foundCity);

                    playerTurn = false;
                }
                else
                {
                    var city = FindCityForWin();

                    if (city == null)
                    {
                        await _speechService.PlaySpeechAsync(client,
                            $"Я не знаю город на букву: {string.Join(" или ", startChars.Select(ConvertCharToStr))}. Вы победили!",
                            $"Я не знаю город на букву: {string.Join(" или ", startChars)}. Вы победили!", cancellationToken: token);
                        return;
                    }

                    usedCitySet.Add(city);

                    startChars = GetLastCharsFromCity(city);

                    await _speechService.PlaySpeechAsync(client, city, cancellationToken: token);
                    await _speechService.PlaySpeechAsync(client,
                        $"Назовите город на букву: {string.Join(" или ", startChars.Select(ConvertCharToStr))}",
                        $"Назовите город на букву: {string.Join(" или ", startChars)}", cancellationToken: token);

                    playerTurn = true;
                }
            }

        }

        private static char[] GetLastCharsFromCity(string foundCity)
        {
            var lastChar = foundCity[^1];

            if (lastChar == 'й' || lastChar == 'ы' || lastChar == 'ь' || lastChar == 'ъ')
                return foundCity[^2..].ToUpper().ToArray();
            else
                return foundCity[^1..].ToUpper().ToArray();
        }

        public static string ConvertCharToStr(char c)
        {
            return c switch
            {
                'Б' => "Бэ",
                'В' => "Вэ",
                'Г' => "Гэ",
                'Д' => "Дэ",
                'Ж' => "Жэ",
                'З' => "Зэ",
                'К' => "Ка",
                'Л' => "Эл",
                'М' => "Эм",
                'Н' => "Эн",
                'П' => "Пэ",
                'Р' => "Рэ",
                'С' => "Сэ",
                'Т' => "Тэ",
                'Ф' => "Фэ",
                'Х' => "Хэ",
                'Ц' => "Цэ",
                'Ч' => "Чэ",
                'Ш' => "Ша",
                'Щ' => "Ща",
                'Ъ' => "Твёрдый знак",
                'Ь' => "Мягкий знак",
                _ => $"{c}"
            };
        }
    }
}
