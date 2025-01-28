﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.AI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading;
using System.Threading.Tasks;

namespace AIDevGallery.Samples.SharedCode;

internal sealed partial class SmartTextBox : Control
{
    private IChatClient? _model;
    private string? _text = string.Empty;
    private CancellationTokenSource? _cts;
    private string? _previousText;
    private string _input = string.Empty;
    private int _selectStart;
    private int _selectEnd;

    private RichEditBox? _inputTextBox;
    private TeachingTip? _aiConfirmTip;
    private TeachingTip? _describeChangesTip;
    private TextBox? _changesInputBox;
    private ProgressBar? _loadingProgressBar;
    private Flyout? _aiFlyout;
    private ListView? _actionFlyoutListView;

    public SmartTextBox()
    {
        this.DefaultStyleKey = typeof(SmartTextBox);
        this.Unloaded += (s, e) => CleanUp();
    }

    public IChatClient Model
    {
        get => (IChatClient)GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _inputTextBox = (RichEditBox)GetTemplateChild("InputTextBox");
        _aiConfirmTip = (TeachingTip)GetTemplateChild("AiConfirmTip");
        _describeChangesTip = (TeachingTip)GetTemplateChild("DescribeChangesTip");
        _changesInputBox = (TextBox)GetTemplateChild("ChangesInputBox");
        _loadingProgressBar = (ProgressBar)GetTemplateChild("LoadingProgressBar");
        _aiFlyout = (Flyout)GetTemplateChild("AIFlyout");
        _actionFlyoutListView = (ListView)GetTemplateChild("FlyoutActionListView");

        _aiConfirmTip.Target = _inputTextBox;
        _describeChangesTip.Target = _inputTextBox;

        _aiConfirmTip.ActionButtonClick += AiConfirmTip_ActionButtonClick;
        _describeChangesTip.ActionButtonClick += DescribeChangesTip_ActionButtonClick;
        _describeChangesTip.CloseButtonClick += DescribeChangesTip_CloseButtonClick;
        _actionFlyoutListView.ItemClick += ActionFlyoutListView_ItemClick;

        _inputTextBox.Document.SetText(TextSetOptions.None, _text);
    }

    private async Task<string> Infer(string systemPrompt, string query, ChatOptions? options = null)
    {
        if (_model == null)
        {
            return string.Empty;
        }

        _cts = new CancellationTokenSource();

        ChatOptions chatOptions = options ?? GenAIModel.GetDefaultChatOptions();

        return (await _model.CompleteAsync(
            [
                new ChatMessage(ChatRole.System, systemPrompt),
                    new ChatMessage(ChatRole.User, query)
            ],
            chatOptions,
            _cts.Token)).Message.Text ?? string.Empty;
    }

    private async Task<string> ChangeToneProfessional(string textToChange)
    {
        string systemPrompt = "You rewrite user-provided writing to adjust the tone of the text. In this case, you will rewrite whatever is provided to sound more \"professional\". When provided with text, respond with only the tone-adjusted version of the text and nothing else. Keep the underlying meaning of the text the same and of around the same length as the source material. Do NOT be overly formal but be polite, succinct, and you MUST USE command MODERN American English. Respond with ONLY the tone-adjusted text and DO NOT provide an explanation, note, or any sort of justification of your changes.";
        ChatOptions chatOptions = GenAIModel.GetDefaultChatOptions();
        chatOptions.MaxOutputTokens = systemPrompt.Length + textToChange.Length;
        return await Infer(systemPrompt, textToChange, chatOptions);
    }

    private async Task<string> ChangeToneCasual(string textToChange)
    {
        string systemPrompt = "You rewrite user-provided writing to adjust the tone of the text. In this case, you will rewrite whatever is provided to sound more \"casual\". When provided with text, respond with only the tone-adjusted version of the text and nothing else. Keep the underlying meaning of the text the same and of around the same length as the source material. Do not be inappropriate but be friendly, succinct, and use every day American English. Respond with ONLY the tone-adjusted text and DO NOT provide an explanation, note, or any sort of justification of your changes.";
        ChatOptions chatOptions = GenAIModel.GetDefaultChatOptions();
        chatOptions.MaxOutputTokens = systemPrompt.Length + textToChange.Length;
        return await Infer(systemPrompt, textToChange, chatOptions);
    }

    private async Task<string> Shorten(string textToChange)
    {
        string systemPrompt = "You change the length of user-provided text to be shorter. When provided with text, respond with only shortened version of the text and nothing else. Maintain the original meaning as much as possible. Respond with ONLY the shortened text and DO NOT provide an explanation, note, or any sort of justification of your changes.";
        ChatOptions chatOptions = GenAIModel.GetDefaultChatOptions();
        chatOptions.MaxOutputTokens = systemPrompt.Length + textToChange.Length;
        return await Infer(systemPrompt, textToChange, chatOptions);
    }

    private async Task<string> Lengthen(string textToChange)
    {
        string systemPrompt = "You change the length of user-provided text to be longer. When provided with text, respond with only lengthened version of the text and nothing else. Maintain the original meaning as much as possible. Respond with ONLY the lengthened text and DO NOT provide an explanation, note, or any sort of justification of your changes.";
        ChatOptions chatOptions = GenAIModel.GetDefaultChatOptions();
        chatOptions.MaxOutputTokens = systemPrompt.Length + 2 * textToChange.Length;
        return await Infer(systemPrompt, textToChange, chatOptions);
    }

    private async Task<string> Proofread(string textToChange)
    {
        string systemPrompt = "You proofread user-provided text to remove spelling mistakes and grammar mistakes. When provided with text, respond with only spelling and grammar corrected text and nothing else. Maintain the original meaning as much as possible. Respond with ONLY the corrected text and DO NOT provide an explanation, note, or any sort of justification of your changes.";
        ChatOptions chatOptions = GenAIModel.GetDefaultChatOptions();
        chatOptions.MaxOutputTokens = systemPrompt.Length + textToChange.Length;
        return await Infer(systemPrompt, textToChange, chatOptions);
    }

    private async Task<string> DescribeChanges(string textToChange, string changes)
    {
        string systemPrompt = "You apply user-defined changes to text. When provided with text, apply the described changes to the text. Respond with only the changed text. Respond with ONLY the changed text and DO NOT provide an explanation , note, any sort of justification of your changes. The changes are: " + changes + ". The provided text is: " + textToChange;
        ChatOptions chatOptions = GenAIModel.GetDefaultChatOptions();
        chatOptions.MaxOutputTokens = systemPrompt.Length + textToChange.Length;
        return await Infer(systemPrompt, textToChange, chatOptions);
    }

    private void CleanUp()
    {
        CancelGeneration();
        _model?.Dispose();
    }

    private void CancelGeneration()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    public void SetText(string text)
    {
        _inputTextBox!.Document.SetText(TextSetOptions.None, text);
    }

    private async void ActionFlyoutListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        ITextSelection selection = _inputTextBox!.Document.Selection;
        _selectStart = selection.StartPosition;
        _selectEnd = selection.EndPosition;
        var tag = (e.ClickedItem as FrameworkElement)!.Tag;
        _inputTextBox.Document.GetText(TextGetOptions.None, out _previousText);
        _inputTextBox.IsEnabled = false;
        _aiFlyout!.Hide();
        _loadingProgressBar!.Visibility = Visibility.Visible;
        string output = string.Empty;
        bool clickedDescribeChanges = false;

        selection.GetText(TextGetOptions.None, out _input);

        await Task.Run(async () =>
        {
            switch (tag)
            {
                case "Proofread":
                    output = await Proofread(_input);
                    break;
                case "Professional":
                    output = await ChangeToneProfessional(_input);
                    break;
                case "Casual":
                    output = await ChangeToneCasual(_input);
                    break;
                case "Shorten":
                    output = await Shorten(_input);
                    break;
                case "Lengthen":
                    output = await Lengthen(_input);
                    break;
                case "Describe":
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        _describeChangesTip!.IsOpen = true;
                    });
                    output = string.Empty;
                    clickedDescribeChanges = true;
                    break;
                default:
                    output = string.Empty;
                    break;
            }
        });

        if (output.Length > 0)
        {
            _inputTextBox.Document.Selection.StartPosition = _selectStart;
            _inputTextBox.Document.Selection.EndPosition = _selectEnd;
            _inputTextBox.Document.Selection.SetText(TextSetOptions.None, output);
            _aiConfirmTip!.IsOpen = true;
        }

        if (!clickedDescribeChanges)
        {
            _loadingProgressBar.Visibility = Visibility.Collapsed;
            _inputTextBox.IsEnabled = true;
            _inputTextBox.Document.Selection.StartPosition = 0;
            _inputTextBox.Document.Selection.EndPosition = 0;
        }
    }

    private void AiConfirmTip_ActionButtonClick(TeachingTip sender, object args)
    {
        if (_previousText != null)
        {
            _inputTextBox!.Document.SetText(TextSetOptions.None, _previousText);
            _aiConfirmTip!.IsOpen = false;
        }
    }

    private async void DescribeChangesTip_ActionButtonClick(TeachingTip sender, object args)
    {
        var output = string.Empty;
        sender.IsOpen = false;

        if (_input.Length > 0)
        {
            string changes = _changesInputBox!.Text;
            string textToChange = _input;
            output = await Task<string>.Run(async () =>
            {
                return await DescribeChanges(textToChange, changes);
            });
        }

        if (output.Length > 0)
        {
            _inputTextBox!.Document.Selection.StartPosition = _selectStart;
            _inputTextBox.Document.Selection.EndPosition = _selectEnd;
            _inputTextBox.Document.Selection.SetText(TextSetOptions.None, output);
            _aiConfirmTip!.IsOpen = true;
        }

        _loadingProgressBar!.Visibility = Visibility.Collapsed;
        _changesInputBox!.Text = string.Empty;
        _inputTextBox!.IsEnabled = true;
        _inputTextBox.Document.Selection.StartPosition = 0;
        _inputTextBox.Document.Selection.EndPosition = 0;
    }

    private void DescribeChangesTip_CloseButtonClick(TeachingTip sender, object args)
    {
        _loadingProgressBar!.Visibility = Visibility.Collapsed;
        _changesInputBox!.Text = string.Empty;
        _inputTextBox!.IsEnabled = true;
        _inputTextBox!.Document.Selection.StartPosition = 0;
        _inputTextBox!.Document.Selection.EndPosition = 0;
    }

    public static readonly DependencyProperty ModelProperty = DependencyProperty.Register(
        nameof(Model),
        typeof(IChatClient),
        typeof(SmartTextBox),
        new PropertyMetadata(default(IChatClient), new PropertyChangedCallback(OnModelChanged)));

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(SmartTextBox),
        new PropertyMetadata(default(string), new PropertyChangedCallback(OnTextChanged)));

    private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        IChatClient model = (IChatClient)e.NewValue;
        if (model != null)
        {
            SmartTextBox smartTextBox = (SmartTextBox)d;
            smartTextBox._model = model;
        }
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        string text = (string)e.NewValue;
        if(text != null)
        {
            SmartTextBox smartTextBox = (SmartTextBox)d;
            smartTextBox._text = text;
            smartTextBox._inputTextBox?.Document.SetText(TextSetOptions.None, text);
        }
    }
}