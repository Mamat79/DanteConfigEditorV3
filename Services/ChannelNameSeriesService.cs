using System.Text.RegularExpressions;

namespace DanteConfigEditor.Services;

public sealed record ChannelSeriesValue(int ChannelIndex, string Name);

public static partial class ChannelNameSeriesService
{
    [GeneratedRegex("^(?<prefix>.*?)(?<number>\\d+)(?<suffix>\\s*)$", RegexOptions.CultureInvariant)]
    private static partial Regex NumericSuffixRegex();

    public static IReadOnlyList<ChannelSeriesValue> Extend(
        IReadOnlyList<ChannelSeriesValue> orderedChannels,
        IReadOnlyList<int> seedChannelIndexes,
        int targetChannelIndex)
    {
        ArgumentNullException.ThrowIfNull(orderedChannels);
        ArgumentNullException.ThrowIfNull(seedChannelIndexes);

        ChannelSeriesValue[] seeds = orderedChannels
            .Where(channel => seedChannelIndexes.Contains(channel.ChannelIndex))
            .OrderBy(channel => Array.FindIndex(orderedChannels.ToArray(), candidate => candidate.ChannelIndex == channel.ChannelIndex))
            .ToArray();
        if (seeds.Length < 2)
        {
            throw new InvalidOperationException("Sélectionnez au moins deux canaux nommés pour prolonger une série.");
        }

        int firstPosition = IndexOf(orderedChannels, seeds[0].ChannelIndex);
        int lastPosition = IndexOf(orderedChannels, seeds[^1].ChannelIndex);
        int targetPosition = IndexOf(orderedChannels, targetChannelIndex);
        if (firstPosition < 0 || lastPosition < 0 || targetPosition <= lastPosition)
        {
            throw new InvalidOperationException("Faites glisser la série vers un canal situé après la sélection.");
        }

        if (seeds.Select(seed => IndexOf(orderedChannels, seed.ChannelIndex)).SequenceEqual(Enumerable.Range(firstPosition, seeds.Length)) is false)
        {
            throw new InvalidOperationException("Les canaux de départ doivent être consécutifs.");
        }

        Match firstMatch = NumericSuffixRegex().Match(seeds[0].Name);
        Match secondMatch = NumericSuffixRegex().Match(seeds[1].Name);
        if (!firstMatch.Success || !secondMatch.Success)
        {
            throw new InvalidOperationException("Les deux premiers noms doivent se terminer par un numéro, par exemple Mic 1 et Mic 2.");
        }

        string prefix = firstMatch.Groups["prefix"].Value;
        string suffix = firstMatch.Groups["suffix"].Value;
        if (!string.Equals(prefix, secondMatch.Groups["prefix"].Value, StringComparison.Ordinal)
            || !string.Equals(suffix, secondMatch.Groups["suffix"].Value, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Les noms sélectionnés doivent partager le même préfixe.");
        }

        int firstNumber = int.Parse(firstMatch.Groups["number"].Value);
        int secondNumber = int.Parse(secondMatch.Groups["number"].Value);
        int step = secondNumber - firstNumber;
        if (step == 0)
        {
            throw new InvalidOperationException("La série doit contenir deux numéros différents.");
        }

        for (int index = 2; index < seeds.Length; index++)
        {
            Match match = NumericSuffixRegex().Match(seeds[index].Name);
            int expected = firstNumber + step * index;
            if (!match.Success
                || !string.Equals(prefix, match.Groups["prefix"].Value, StringComparison.Ordinal)
                || !string.Equals(suffix, match.Groups["suffix"].Value, StringComparison.Ordinal)
                || !int.TryParse(match.Groups["number"].Value, out int actual)
                || actual != expected)
            {
                throw new InvalidOperationException("La sélection ne forme pas une série numérique régulière.");
            }
        }

        int numberWidth = seeds
            .Select(seed => NumericSuffixRegex().Match(seed.Name).Groups["number"].Value.Length)
            .Max();
        bool preserveLeadingZeroes = seeds.Any(seed =>
        {
            string value = NumericSuffixRegex().Match(seed.Name).Groups["number"].Value;
            return value.Length > 1 && value.StartsWith('0');
        });

        List<ChannelSeriesValue> result = [];
        int nextNumber = firstNumber + step * seeds.Length;
        for (int position = lastPosition + 1; position <= targetPosition; position++)
        {
            string number = preserveLeadingZeroes
                ? nextNumber.ToString().PadLeft(numberWidth, '0')
                : nextNumber.ToString();
            result.Add(new ChannelSeriesValue(orderedChannels[position].ChannelIndex, $"{prefix}{number}{suffix}"));
            nextNumber += step;
        }

        return result;
    }

    private static int IndexOf(IReadOnlyList<ChannelSeriesValue> channels, int channelIndex)
    {
        for (int index = 0; index < channels.Count; index++)
        {
            if (channels[index].ChannelIndex == channelIndex)
            {
                return index;
            }
        }

        return -1;
    }
}
