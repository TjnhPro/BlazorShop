namespace BlazorShop.Web.SharedV2.Models
{
    using BlazorShop.Web.SharedV2.Toast;

    public class ToastModel
    {
        public string Heading { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string ToastLevel { get; set; } = string.Empty;

        public ToastIcon Icon { get; set; } = ToastIcon.Default;

        public ToastPosition Position { get; set; } = ToastPosition.TopRight;

        public bool Persist { get; set; } = false;

        public int Duration { get; set; } = 5000;

        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();
    }
}
