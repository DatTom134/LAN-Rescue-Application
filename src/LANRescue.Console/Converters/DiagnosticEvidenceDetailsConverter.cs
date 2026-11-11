using System;
using System.Globalization;
using System.Windows.Data;
using LANRescue.ConsoleApp.Services;
using LANRescue.Domain.Entities;
using LANRescue.Domain.Enums;

namespace LANRescue.ConsoleApp.Converters;

/// <summary>Produces localized diagnostic-result summaries while retaining raw details in the tooltip.</summary>
public sealed class DiagnosticEvidenceDetailsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DiagnosticEvidence evidence)
            return string.Empty;

        string key = evidence.Outcome switch
        {
            DiagnosticProbeOutcome.Succeeded => evidence.ProbeType switch
            {
                DiagnosticProbeType.GatewayPing => "Diagnostics_Detail_IcmpSucceeded",
                DiagnosticProbeType.TcpConnection => "Diagnostics_Detail_TcpSucceeded",
                DiagnosticProbeType.DnsResolution => "Diagnostics_Detail_DnsSucceeded",
                _ => "Diagnostics_Detail_Succeeded"
            },
            DiagnosticProbeOutcome.TimedOut => "Diagnostics_Detail_TimedOut",
            DiagnosticProbeOutcome.ConnectionRefused => "Diagnostics_Detail_ConnectionRefused",
            DiagnosticProbeOutcome.NameNotFound => "Diagnostics_Detail_NameNotFound",
            DiagnosticProbeOutcome.NoRecords => "Diagnostics_Detail_NoRecords",
            DiagnosticProbeOutcome.Cancelled => "Diagnostics_Detail_Cancelled",
            DiagnosticProbeOutcome.Skipped => "Diagnostics_Detail_Skipped",
            DiagnosticProbeOutcome.Failed => "Diagnostics_Detail_Failed",
            _ => "Diagnostics_Detail_Error"
        };

        return evidence.ProbeType == DiagnosticProbeType.GatewayPing &&
               evidence.Outcome == DiagnosticProbeOutcome.Succeeded
            ? LocalizationManager.Format(key, evidence.DurationMs ?? 0)
            : LocalizationManager.GetString(key);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
