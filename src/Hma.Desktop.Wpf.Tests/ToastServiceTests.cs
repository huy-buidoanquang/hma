using Hma.Desktop.Wpf.Infrastructure.Notifications;

namespace Hma.Desktop.Wpf.Tests;

public class ToastServiceTests
{
    [Fact]
    public void Show_and_dismiss_publish_current_toast_state()
    {
        var toast = new ToastService(TimeProvider.System);

        toast.Show("Không thể xóa.", isError: true);

        Assert.Equal("Không thể xóa.", toast.Message);
        Assert.True(toast.IsError);

        toast.Dismiss();

        Assert.Null(toast.Message);
    }
}
