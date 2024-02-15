using Microsoft.AspNetCore.SignalR;
using SpeechWebGame.Tools;
using System.Runtime.Versioning;

namespace SpeechWebGame.Games
{
    [SupportedOSPlatform("windows")]
    public class MathGame : ISpeechGame
    {
        private readonly SpeechService _speechService;
        private readonly InputService _inputService;

        public MathGame(SpeechService speechService, InputService inputService)
        {
            _speechService = speechService;
            _inputService = inputService;
        }

        public static GameInfo GameInfo { get; } = new()
        {
            Id = "math",
            MinPlayers = 1,
            MaxPlayers = 1,
            Title = "Проверка таблицы умножения"
        };

        public async Task RunAsync(ISingleClientProxy client, CancellationToken token)
        {
            var pairs = new HashSet<(int, int)>();
            for (int i = 2; i < 10; i++)
            {
                for (int j = 2; j < 10; j++)
                {
                    if (Math.Abs(i) <= 1 || Math.Abs(j) <= 1)
                        continue;
                    pairs.Add((Math.Min(i, j), Math.Max(i, j)));
                }
            }

            var rand = new Random();
            var list = pairs.OrderBy(_ => rand.Next()).Take(10).ToArray();

            string? name;
            while (true)
            {
                await _speechService.PlaySpeechAsync(client, "Как вас зовут?", rate: 2, cancellationToken: token);

                bool TestFunc(string str, TimeSpan time)
                {
                    return str.Length > 1 && time > TimeSpan.FromSeconds(2);
                }

                var nameResult = await _inputService.GetInputAsync(client, true, TimeSpan.FromSeconds(5), TestFunc, token);
                name = nameResult.FirstOrDefault()?.Split(' ').FirstOrDefault();
                if (name == null)
                    continue;

                if (name.Length < 2)
                    continue;

                name = $"{char.ToUpper(name[0])}{name.Substring(1)}";

                await _speechService.PlaySpeechAsync(client, $"Вас зовут {name}. Верно?", rate: 2, cancellationToken: token);

                var yesResult = await _inputService.GetInputAsync(client, true, TimeSpan.FromSeconds(10), null, default, "да", "нет", "верно");
                if (yesResult.Any(q => q.Equals("да", StringComparison.CurrentCultureIgnoreCase)
                    || q.Equals("верно", StringComparison.CurrentCultureIgnoreCase)))
                {
                    break;
                }
            }

            await _speechService.PlaySpeechAsync(client, $"Начинаем игру", rate: 3, cancellationToken: token);

            foreach (var (a, b) in list)
            {
                await _speechService.PlaySpeechAsync(client, $"{RusNumber.Str(a)} умножить на {RusNumber.Str(b)}", rate: 3, cancellationToken: token);

                var successResult = a * b;
                var successResultStr = RusNumber.Str(successResult);

                var successVariants = new[] { successResultStr, successResult.ToString() };

                bool TestResult(string text)
                {
                    return successVariants.Any(q => q.Equals(text, StringComparison.CurrentCultureIgnoreCase));
                }

                var result = await _inputService.GetInputAsync(client, true, TimeSpan.FromSeconds(10),
                    (s, span) => TestResult(s) || successVariants.All(q => q.Length < s.Length || !q.StartsWith(s)), token);

                if (result.Any(TestResult))
                {
                    await _speechService.PlaySpeechAsync(client, "Правильно", rate: 3, cancellationToken: token);
                }
                else if (result.All(q => q == ""))
                {
                    await _speechService.PlaySpeechAsync(client, $"Вы не успели ответить. Верный ответ: {successResult}", rate: 3, cancellationToken: token);
                }
                else
                {
                    await _speechService.PlaySpeechAsync(client, $"Ошибка! Верный ответ: {successResult}", rate: 3, cancellationToken: token);
                }
            }

            await _speechService.PlaySpeechAsync(client, $"Cпасибо за игру, {name}", cancellationToken: token);
        }
    }
}
