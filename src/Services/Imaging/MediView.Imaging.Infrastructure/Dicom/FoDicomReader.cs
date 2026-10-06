using System.Text.RegularExpressions;
using FellowOakDicom;
using MediView.Imaging.Application.Instances;

namespace MediView.Imaging.Infrastructure.Dicom;

internal sealed partial class FoDicomReader : IDicomReader
{
    private const int MaxUidLength = 64;

    public async Task<DicomHeader?> ReadHeaderAsync(Stream content, CancellationToken cancellationToken)
    {
        DicomFile file;
        try
        {
            file = await DicomFile.OpenAsync(content, FileReadOption.SkipLargeTags);
        }
        catch (DicomException)
        {
            return null;
        }

        var dataset = file.Dataset;
        var seriesInstanceUid = dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty);
        var sopInstanceUid = dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty);
        var instanceNumber = dataset.GetSingleValueOrDefault(DicomTag.InstanceNumber, 0);

        return IsUid(seriesInstanceUid) && IsUid(sopInstanceUid)
            ? new DicomHeader(seriesInstanceUid, sopInstanceUid, instanceNumber)
            : null;
    }

    private static bool IsUid(string value) => value.Length <= MaxUidLength && UidPattern().IsMatch(value);

    [GeneratedRegex(@"^[0-9]+(\.[0-9]+)*$")]
    private static partial Regex UidPattern();
}
