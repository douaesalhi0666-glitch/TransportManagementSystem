using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace TransportManagementSystem.Services
{
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _fromEmail;
        private readonly string _fromPassword;

        public EmailService()
        {
            // Configure with your email settings (example with Gmail)
            _smtpServer = "smtp.gmail.com";
            _smtpPort = 587;
            _fromEmail = "sewstms@gmail.com"; // Change this!
            _fromPassword = "sewsTransportManagementSystem"; // Change this!
        }

        public async Task<bool> SendResetPasswordEmail(string toEmail, string userName, string resetLink)
        {
            try
            {
                using (var client = new SmtpClient(_smtpServer, _smtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(_fromEmail, _fromPassword);

                    var subject = "Réinitialisation de votre mot de passe";
                    var body = $@"
                        <html>
                        <body>
                            <h2>Bonjour {userName},</h2>
                            <p>Vous avez été inscrit sur la plateforme de gestion de transport.</p>
                            <p>Veuillez cliquer sur le lien ci-dessous pour créer votre mot de passe :</p>
                            <a href='{resetLink}' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
                                Créer mon mot de passe
                            </a>
                            <p>Ce lien expirera dans 24 heures.</p>
                            <p>Si vous n'avez pas demandé cette action, ignorez cet email.</p>
                            <hr />
                            <p>Merci,<br/>L'équipe de transport</p>
                        </body>
                        </html>";

                    var mailMessage = new MailMessage(_fromEmail, toEmail, subject, body);
                    mailMessage.IsBodyHtml = true;

                    await client.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendWelcomeEmail(string toEmail, string userName)
        {
            try
            {
                using (var client = new SmtpClient(_smtpServer, _smtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(_fromEmail, _fromPassword);

                    var subject = "Bienvenue sur la plateforme de transport";
                    var body = $@"
                        <html>
                        <body>
                            <h2>Bienvenue {userName} !</h2>
                            <p>Votre compte a été créé avec succès.</p>
                            <p>Un administrateur vous contactera bientôt avec vos identifiants de connexion.</p>
                            <hr />
                            <p>Merci,<br/>L'équipe de transport</p>
                        </body>
                        </html>";

                    var mailMessage = new MailMessage(_fromEmail, toEmail, subject, body);
                    mailMessage.IsBodyHtml = true;

                    await client.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email error: {ex.Message}");
                return false;
            }
        }
        
    }
}