using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using inferenceclinet.Models;
using inferenceclinet.Services;

namespace inferenceclinet.Views;

public partial class MonitorWindow : Window
{
    private readonly ObservableCollection<InspectionRecord> _history = new();

    // _history와 같은 인덱스로 1:1 대응 (검사 결과마다 하나씩 추가됨).
    private readonly List<CheckErrorMetaData> _errorMetadata = new();

    private readonly IRequestService _requestService;

    private CameraCaptureService? _camera;
    private CancellationTokenSource? _captureCts;

    private readonly DispatcherTimer _elapsedTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private DateTime _analysisStartTime;

    public MonitorWindow()
    {
        InitializeComponent();
        HistoryGrid.ItemsSource = _history;
        _requestService = new RequestService(new HttpClient { BaseAddress = new Uri(AppConfig.MainServerBaseUrl) });
        _elapsedTimer.Tick += (_, _) => ElapsedText.Text = (DateTime.Now - _analysisStartTime).ToString(@"hh\:mm\:ss");
    }

    private void OnAnalyzeClick(object sender, RoutedEventArgs e)
    {
        if (_captureCts is not null)
        {
            return; // 이미 실행 중
        }

        try
        {
            _camera = new CameraCaptureService();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "카메라 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        CamPlaceholderText.Visibility = Visibility.Collapsed;
        _captureCts = new CancellationTokenSource();
        _ = RunCaptureLoopAsync(_captureCts.Token);

        _analysisStartTime = DateTime.Now;
        ElapsedText.Text = "00:00:00";
        _elapsedTimer.Start();
    }

    private void OnStopClick(object sender, RoutedEventArgs e)
    {
        _captureCts?.Cancel();
        _captureCts = null;

        _camera?.Dispose();
        _camera = null;

        CamPlaceholderText.Visibility = Visibility.Visible;
        _elapsedTimer.Stop();
    }

    // 03-continuous-capture-plan.md 3절: 같은 clientId로 여러 요청이 동시에 대기하면
    // MainServer가 응답을 뒤섞을 수 있어, 한 프레임 보내고 응답(or 타임아웃)까지 기다린 뒤 다음 프레임을 보낸다.
    private async Task RunCaptureLoopAsync(CancellationToken ct)
    {
        var frameNumber = 0;

        while (!ct.IsCancellationRequested)
        {
            var captured = _camera?.CaptureFrame();
            if (captured is null)
            {
                await DelayIgnoringCancel(ct);
                continue;
            }

            var (jpeg, preview) = captured.Value;
            CamImage.Source = preview;

            frameNumber++;
            var request = new RequestMessage
            {
                Client = AppConfig.NumericClientId,
                Filename = $"frame_{frameNumber}.jpg",
                Filelastnumber = frameNumber,
                Filelength = jpeg.Length,
                Filedata = Convert.ToBase64String(jpeg)
            };

            try
            {
                var waitTask = ClientHttpHost.WaitForMainResponseAsync(
                    AppConfig.NumericClientId.ToString(),
                    TimeSpan.FromMilliseconds(AppConfig.ResponseWaitTimeoutMs));

                await _requestService.SendAsync(request, ct);
                var response = await waitTask;

                HandleResponse(response, preview);
            }
            catch (HttpRequestException)
            {
                DefectStatusText.Text = "서버 연결 실패";
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await DelayIgnoringCancel(ct);
        }
    }

    private void HandleResponse(MainResponsePayload? response, BitmapSource capturedFrame)
    {
        if (response is null)
        {
            DefectStatusText.Text = "응답 없음(시간 초과)";
            return;
        }

        var isSuccess = string.Equals(response.SucessRate, "sucess", StringComparison.OrdinalIgnoreCase);
        DefectStatusText.Text = isSuccess ? "정상" : "불량";

        var boxText = response.Box is { Length: 4 } box
            ? $"{box[0]:0.0}, {box[1]:0.0}, {box[2]:0.0}, {box[3]:0.0}"
            : "-";

        var confidenceText = response.Confidence is { } confidence
            ? $"{confidence:P1}"
            : "-";

        _history.Add(new InspectionRecord(
            DateTime.Now,
            isSuccess ? "정상" : "불량",
            Confidence: confidenceText,
            DefectType: response.ProductName,
            Box: boxText,
            Image: capturedFrame));

        var boxInts = response.Box is { Length: 4 } boxValues
            ? new[]
            {
                (int)Math.Round(boxValues[0]),
                (int)Math.Round(boxValues[1]),
                (int)Math.Round(boxValues[2]),
                (int)Math.Round(boxValues[3]),
            }
            : null;

        _errorMetadata.Add(new CheckErrorMetaData
        {
            box = boxInts,
            trust = (float)(response.Confidence ?? 0),
            product = response.ProductName,
            timestamp = DateTime.Now,
        });
    }

    private static async Task DelayIgnoringCancel(CancellationToken ct)
    {
        try
        {
            await Task.Delay(AppConfig.CaptureIntervalMs, ct);
        }
        catch (OperationCanceledException)
        {
            // 중지 버튼으로 인한 취소는 무시하고 루프 쪽에서 종료 처리
        }
    }

    private void OnHistoryRowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var index = HistoryGrid.SelectedIndex;
        if (index >= 0 && HistoryGrid.SelectedItem is InspectionRecord record)
        {
            new DefectDetailWindow(record, _errorMetadata, index).Show();
        }
    }

    // 표에서 더블클릭하지 않아도 선택된(또는 가장 최근) 이력의 상세 화면으로 바로 이동.
    private void OnViewDetailClick(object sender, RoutedEventArgs e)
    {
        var index = HistoryGrid.SelectedIndex >= 0 ? HistoryGrid.SelectedIndex : _history.Count - 1;
        if (index < 0)
        {
            MessageBox.Show("아직 검사 이력이 없습니다.", "상세보기", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        new DefectDetailWindow(_history[index], _errorMetadata, index).Show();
    }
}
