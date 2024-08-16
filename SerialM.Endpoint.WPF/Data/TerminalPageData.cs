using SerialM.Business.Exceptions;
using SerialM.Business.Utilities;
using SerialM.Endpoint.WPF.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;

namespace SerialM.Endpoint.WPF.Data
{
    public class TerminalPageData
    {
        #region Statics Strings
        public static readonly string TIME_RESOURCEKEY = "TimeStyle";

        public static readonly string DEFAULT_RESOURCEKEY = "DefaultTextStyle";
        public static readonly string SUCCESS_RESOURCEKEY = "SuccessStyle";
        public static readonly string FAIL_RESOURCEKEY = "FailStyle";
        public static readonly string INFO_RESOURCEKEY = "InfoStyle";
        public static readonly string RECEIVE_RESOURCEKEY = "ReceiveStyle";
        public static readonly string SENT_RESOURCEKEY = "SentStyle";

        public static readonly string TEXTBOX_FAIL_RESOURCEKEY = "TextBoxFailStyle";
        public static readonly string TEXTBOX_SUCCESS_RESOURCEKEY = "TextBoxSuccessStyle";
        #endregion


        #region Properties
        private Task _autoRunTask;
        public Task AutoRunTask
        {
            get => _autoRunTask;
            set => _autoRunTask = value;
        }

        private CancellationTokenSource _cancellationTokenSource;
        public CancellationTokenSource CancellationTokenSource
        {
            get => _cancellationTokenSource;
            set => _cancellationTokenSource = value;
        }

        private ObservableCollection<SendListViewItem> _sendListViewItems;
        public virtual ObservableCollection<SendListViewItem> SendItems
        {
            get => _sendListViewItems;
            set
            {
                this._sendListViewItems = value;
                _sendListView.ItemsSource = this._sendListViewItems;
            }
        }

        public bool IsAutoSendRunning => _autoRunTask != null && !_autoRunTask.IsCompleted;
        public bool IsAutoSendCompleted => _autoRunTask == null || _autoRunTask.IsCompleted;


        private List<TextboxItem> _textRanges = new();
        public List<TextboxItem> TextRanges
        {
            get => _textRanges;
        }

        private int _hex_limit = 1000;
        public int Hex_limit
        {
            get => _hex_limit;
            set => _hex_limit = value;
        }


        private bool _scrollToEnd = false, _hexText = false;
        public bool ScrollToEnd
        {
            get => _scrollToEnd;
            set => _scrollToEnd = value;
        }
        public bool HexText
        {
            get => _hexText;
            set => _hexText = value;
        }

        #endregion


        private RichTextBox _richTextBox;
        private Paragraph mainParagraph = new();
        private Page _page;
        private ListView _sendListView;
        private ProgressBar _progressBar;
        private Dispatcher _dispatcher;


        public TerminalPageData(Page page, RichTextBox richTextBox, ListView sendListView, ProgressBar progressBar)
        {
            _page = page;
            _richTextBox = richTextBox;
            _sendListView = sendListView;
            this.mainParagraph = new();
            _dispatcher = _page.Dispatcher;
            _progressBar = progressBar;
            Initialize();
        }

        public void Initialize()
        {
            InitializeTextBox();
            InitializeListView();
        }


        private void InitializeListView()
        {
            SendItems = [new SendListViewItem()];
            _sendListView.ItemsSource = SendItems;
        }

        private void InitializeTextBox()
        {
            _richTextBox.Document.Blocks.Clear();
            _richTextBox.Document.Blocks.Add(mainParagraph);
        }

        public void ClearTexts()
        {
            _dispatcher.Invoke(() =>
            {
                mainParagraph.Inlines.Clear();
                TextRanges.Clear();
            });
        }

        public void AddNewSendItem()
        {
            if (IsAutoSendCompleted)
                SendItems.Add(new SendListViewItem());
            else
                throw new NotCompletedTaskException("Auto run is not completed.");
        }

        public void AppendLineToRichTextBox(string text, string colorResourceKey = "", string dateTime = "")
        {
            if (HexText)
                text = text.ToHex();/*.Replace("0A", "\r");*/

            if (dateTime == "")
                dateTime = DateTime.Now.ToString("HH:mm:ss:ffff");



            var start = _richTextBox.Document.ContentEnd;
            Run run = new Run(text, _richTextBox.Document.ContentEnd);
            Run timeInline = new Run(dateTime);
            var runTime = new Span(timeInline, _richTextBox.Document.ContentEnd);

            if (string.IsNullOrEmpty(colorResourceKey))
                colorResourceKey = DEFAULT_RESOURCEKEY;

            runTime.Style = (Style)_page.FindResource(TIME_RESOURCEKEY);

            runTime.Inlines.Add(new Run(" : "));
            var span = new Span(runTime, start);
            span.Inlines.Add(run);
            span.Inlines.Add(new Run("\r"));
            span.Style = (Style)_page.FindResource(colorResourceKey);

            //DataTextBox.Document.Blocks.Add(paragraph);
            mainParagraph.Inlines.Add(span);

            _textRanges.Add(new TextboxItem { TextRun = run, TimeRun = timeInline, ResourceKey = colorResourceKey });

            if (ScrollToEnd)
                _richTextBox.ScrollToEnd();
        }

        public async void TaskTextProccess(Func<string, string> textProccess)
        {
            await Task.Run(() =>
            {
                SetSBar(TextRanges.Count, 0, 0);

                int last = TextRanges.Count - 2000;
                if (last < 0) last = 0;
                for (int i = TextRanges.Count - 1; i >= last; i--)
                {
                    var txt = TextRanges[i].TextRun;
                    _dispatcher.Invoke(() =>
                    {
                        _progressBar.Value++;
                        txt.Text = textProccess(txt.Text);
                    });
                }
                HideSbar();
            });
        }

        public void HideSbar()
        {
            _dispatcher.Invoke(() =>
            {
                _progressBar.Visibility = Visibility.Hidden;
            });
        }

        public void SetSBar(int max, int min = 0, int val = 0)
        {
            _dispatcher.Invoke(() =>
            {
                _progressBar.Visibility = Visibility.Visible;
                _progressBar.Minimum = min;
                _progressBar.Maximum = max;
                _progressBar.Value = val;
            });
        }

        /// <summary>
        /// Starts the auto send process asynchronously. 
        /// This method iterates through a collection of items, sending each one with an optional delay, while allowing for cancellation of the process. 
        /// It accepts delegates to handle custom actions at the start, during the sending of each item, and at the end of the process.
        /// </summary>
        /// <param name="sendData">Action to send the item's text data.</param>
        /// <param name="onStart">Action to execute when the auto send process starts.</param>
        /// <param name="onEnd">Action to execute when the auto send process ends, whether completed or canceled.</param>

        public async Task StartAutoSendItems(Action<string> sendData, Action onStart, Action onEnd)
        {
            if (IsAutoSendRunning)
            {
                throw new AutoSendAlreadyRunningException();
            }

            // Initialize the CancellationTokenSource
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            //AutoRunBtn.Content = "Cancel"; // Change button text to "Cancel"
            //Dispatcher.Invoke(() => RemoveBtn.IsEnabled = false);
            onStart();

            // Create and start the task
            _autoRunTask = Task.Run(async () =>
            {
                try
                {
                    foreach (var item in SendItems)
                    {
                        cancellationToken.ThrowIfCancellationRequested(); // Check for cancellation

                        if (!item.CanSend) continue;

                        if (item.Delay > 0)
                            await Task.Delay(item.Delay, cancellationToken); // Pass the cancellation token

                        _dispatcher.Invoke(() => sendData(item.Text));
                        _dispatcher.Invoke(() => _sendListView.SelectedItem = item);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Handle the cancellation
                }
                finally
                {
                    //    RemoveBtn.IsEnabled = true;
                    //    AutoRunBtn.Content = "Auto Send";
                    //    AutoRunBtn.IsEnabled = true;
                    onEnd();
                }
            }, cancellationToken);

            await _autoRunTask;
        }

        /// <summary>
        /// Cancels the auto send process.
        /// </summary>
        /// <param name="endAutoSend">Ends the auto send process and resets UI state.</param>
        /// <returns></returns>
        public void CancelAutoSendItems(Action onCancel)
        {
            if (_cancellationTokenSource != null && IsAutoSendRunning)
            {
                _cancellationTokenSource.Cancel();

                //AutoRunBtn.IsEnabled = false; // Disable button to prevent multiple clicks during cancellation
                onCancel();
            }
            else if (_cancellationTokenSource == null)
                throw new NullReferenceException("CancellationTokenSource is Null");            
        }
    }
}
