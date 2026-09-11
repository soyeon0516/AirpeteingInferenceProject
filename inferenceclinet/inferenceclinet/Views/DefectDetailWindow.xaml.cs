using System.Windows;
using inferenceclinet.Models;

namespace inferenceclinet.Views;

public partial class DefectDetailWindow : Window
{
    private readonly List<CheckErrorMetaData> _errorMetadata;
    private readonly int _metadataIndex;

    public DefectDetailWindow(InspectionRecord record, List<CheckErrorMetaData> errorMetadata, int metadataIndex)
    {
        InitializeComponent();

        _errorMetadata = errorMetadata;
        _metadataIndex = metadataIndex;

        TimeText.Text = record.Time.ToString("yyyy-MM-dd HH:mm:ss");
        DefectTypeText.Text = record.DefectType ?? "-";
        ConfidenceText.Text = record.Confidence ?? "-";
        BoxText.Text = record.Box ?? "-";

        if (record.Image is not null)
        {
            DetailImage.Source = record.Image;
            ImagePlaceholderText.Visibility = Visibility.Collapsed;
        }

        if (_metadataIndex >= 0 && _metadataIndex < _errorMetadata.Count)
        {
            MemoTextBox.Text = _errorMetadata[_metadataIndex].memo ?? string.Empty;
        }
    }

    private void OnRegisterClick(object sender, RoutedEventArgs e)
    {
        if (_metadataIndex >= 0 && _metadataIndex < _errorMetadata.Count)
        {
            _errorMetadata[_metadataIndex].memo = MemoTextBox.Text;
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
