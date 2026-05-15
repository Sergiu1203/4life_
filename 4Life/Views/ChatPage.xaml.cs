using _4Life.ViewModels;

namespace _4Life.Views;

public partial class ChatPage : ContentPage
{
    private readonly ChatViewModel _viewModel;

    public ChatPage(ChatViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadMessages();

        // Scroll automat la ultimul mesaj
        if (_viewModel.Messages.Count > 0)
            MessagesCollection.ScrollTo(_viewModel.Messages[^1], position: ScrollToPosition.End, animate: false);
    }
}
