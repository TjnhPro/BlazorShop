namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Messages;

    using MailKit.Net.Smtp;

    using MimeKit;

    public sealed class MailKitStoreEmailTransportSender : IStoreEmailTransportSender
    {
        public async Task SendAsync(
            StoreEmailTransportSettings transport,
            string toEmail,
            string subject,
            string bodyHtml,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(transport);

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(transport.FromName ?? string.Empty, transport.FromEmail));
            if (!string.IsNullOrWhiteSpace(transport.ReplyToEmail))
            {
                email.ReplyTo.Add(MailboxAddress.Parse(transport.ReplyToEmail));
            }

            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new BodyBuilder
            {
                HtmlBody = bodyHtml,
            }.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                transport.SmtpHost,
                transport.SmtpPort,
                transport.UseSsl,
                cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(transport.Username)
                && !string.IsNullOrWhiteSpace(transport.Password))
            {
                await smtp.AuthenticateAsync(
                    transport.Username,
                    transport.Password,
                    cancellationToken).ConfigureAwait(false);
            }

            await smtp.SendAsync(email, cancellationToken).ConfigureAwait(false);
            await smtp.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
        }
    }
}
