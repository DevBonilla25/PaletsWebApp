using NuGet.Protocol.Plugins;
using System.Net;
using System.Text;
using System;
using System.Text.Json;
using System.Net.Mail;
using Google.Apis.Auth.OAuth2;


namespace PaletsWebApp.Utilites
{
    public class Utils
    {
        private static readonly HttpClient HttpClientInstance = new HttpClient();

        //public static string applicationID = "AAAAMlxmOFM:APA91bEXj6-nCmKUs4_GbAUzMN62e3iL6RsV6zDLQwdEkELSrlWvPqYc4EReuaHoC5hyhC5K3X__EGBAAUyU0P9BV4qm9d4-iSh9Am8Ks6dDzBPxhd_1XgR0XygsLjOK4BIvJz1c7-tW";
        //public static string senderId = "216298567763";

        public static string BadgeTransfer(string cad)
        {

            //https://www.studytonight.com/bootstrap/bootstrap-badge

            string ret = "";

            if (cad.ToLower() == "por recibir" || cad.ToLower() == "por reclamar") { ret = "bg-secondary"; }
            else if (cad.ToLower() == "recibido" || cad.ToLower() == "reclamado") { ret = "bg-success"; }
            else if (cad.ToLower() == "rechazado") { ret = "bg-danger"; }
            else if (cad.ToLower() == "anulado") { ret = "bg-danger"; }
            else if (cad.ToLower() == "procesado parcialmente") { ret = "bg-warning text-dark"; }
            else { ret = "bg-primary"; }

            return ret;

        }

        public static string BadgePalet(string cad)
        {

            //https://www.studytonight.com/bootstrap/bootstrap-badge

            string ret = "";

            if (cad.ToLower() == "disponible" || cad.ToLower() == "reclamado") { ret = "bg-primary"; }
            else if (cad.ToLower() == "en transferencia" || cad.ToLower() == "en reclamo") { ret = "bg-warning"; }
            else if (cad.ToLower() == "dado de baja") { ret = "bg-danger"; }

            return ret;

        }
        public static string BadgeUser(bool? cad)
        {

            //https://www.studytonight.com/bootstrap/bootstrap-badge

            string ret = "";

            if (cad == true) { ret = "bg-success"; }
            else if (cad == false) { ret = "bg-danger"; }

            return ret;

        }

        public static string EstatusUser(bool? val)
        {
            return val == true ? "Activo" : "Inactivo";
        }

        //public static string FormatDate(DateTime? dt)
        //eSTE CODIGO ESTABA PRIMERAMENTE (NO MOSTRABA LAS HORAS CORRECTAS)
        //{
        //    return ((DateTime)dt).ToString("dd/MMM/yyyy HH:mm");
        //}


        //convertir la fecha almacenada en UTC a la zona horaria de Ecuador (UTC-5) antes de formatearla, utilizando TimeZoneInfo.
        public static string FormatDate(DateTime? dt)
        {
            if (dt == null)
            {
                return "Sin fecha";
            }

            // Obtener la zona horaria de Ecuador (UTC-5)
            var ecuadorTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");

            // Convertir la fecha directamente desde UTC a la zona horaria de Ecuador
            var localTime = TimeZoneInfo.ConvertTimeFromUtc((DateTime)dt, ecuadorTimeZone);

            return localTime.ToString("dd/MMM/yyyy HH:mm");
        }






        public static async Task SendNotification(string? deviceId,
                                             string email,
                                             string nombre,
                                             string titulo,
                                             string mensaje)
        {

            string resPush = "";
            // string resEmail = "";

            if (!string.IsNullOrEmpty(deviceId))
            {
                resPush = await SendPushNotification(deviceId,titulo,mensaje);
            }


            // Envío temporalmente deshabilitado: el SMTP síncrono retrasaba la respuesta al usuario.
            // resEmail = SendEmailNotification(email,nombre,titulo, mensaje);

        
        }


        public static string SendEmailNotification(string email,
                                                   string nombre,
                                                   string titulo,
                                                   string mensaje)
        {

            string msgTemplateEmail =
            @"<table>
                <tr>
                    <td>
                        <img src='http://equipoti-001-site1.ftempurl.com/logomini.png'>
                    </td>
                    <td style='padding:10px;'>
                        <span style='font-size:24px!important; font-weight: bold;'>
                            Ferreteria Bonilla
                        </span>
                    </td>
               </tr>
               <tr>
                    <td colspan='2'>
                        Estimado(a) {0} 
                    </td>
                </tr>
                <tr>
                    <td colspan='2'>
                        <p style='padding:10px; font-size:16px!important; font-weight: bold;'> {1} </p>
                    </td>
                </tr>
            </table>";

            string msgEmail = string.Format(msgTemplateEmail, nombre, mensaje);


            try
            {

                //https://learn.microsoft.com/en-us/answers/questions/1167393/send-email-form-gmail-account-using-c

                using (var client = new SmtpClient())
                {
                    client.Host = "smtp.gmail.com";
                    client.Port = 587;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential("seminariosit52@gmail.com", "xasvzsuuiwhkgjjz");
                    using (var message = new MailMessage(
                        from: new MailAddress("seminariosit52@gmail.com", "Ferreteria Bonilla"),
                        to: new MailAddress(email, nombre)
                        //to: new MailAddress("hernanjls@gmail.com", nombre)
                        ))
                    {

                        message.Subject = titulo;
                        message.Body = msgEmail;
                        message.IsBodyHtml = true;

                        client.Send(message);
                    }
                }


            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }

            



            return "";
           
        }

        public static async Task<string> SendPushNotification(string? deviceId,
                                                      string titulo,
                                                      string mensaje)
        {
            string response;

            try
            {
                // Obtén el token de acceso (puedes almacenarlo en caché para mejorar el rendimiento)
                var accessToken = await GetAccessToken();  // Ya no necesitas pasar GoogleCredential aquí.

                // Define la URL de la API de FCM V1
                string fcmUrl = "https://fcm.googleapis.com/v1/projects/portal-bonilla/messages:send";

                // Crea el payload del mensaje
                var data = new
                {
                    message = new
                    {
                        token = deviceId,
                        notification = new
                        {
                            body = mensaje,
                            title = titulo
                        },
                        android = new
                        {
                            priority = "HIGH",
                            notification = new
                            {
                                sound = "default"
                            }
                        },
                        data = new
                        {
                            customData = "additional data"
                        }
                    }
                };

                var json = JsonSerializer.Serialize(data);
                using var request = new HttpRequestMessage(HttpMethod.Post, fcmUrl);
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using var responseFCM = await HttpClientInstance.SendAsync(request);
                response = await responseFCM.Content.ReadAsStringAsync();
                if (!responseFCM.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"FCM respondió {(int)responseFCM.StatusCode}: {response}");
                }
            }
            catch (Exception ex)
            {
                response = ex.Message;
                Console.Error.WriteLine($"No se pudo enviar la notificación FCM: {ex.Message}");
            }

            return response;
        }

        public static async Task<string> GetAccessToken()
        {
            var credential = GetFirebaseCredential()
                .CreateScoped("https://www.googleapis.com/auth/cloud-platform");

            return await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
        }

        public static GoogleCredential GetFirebaseCredential()
        {
            var credentialBase64 = Environment.GetEnvironmentVariable(
                "FIREBASE_SERVICE_ACCOUNT_BASE64");
            if (!string.IsNullOrWhiteSpace(credentialBase64))
            {
                try
                {
                    var json = Encoding.UTF8.GetString(
                        Convert.FromBase64String(credentialBase64.Trim()));
                    return GoogleCredential.FromJson(json);
                }
                catch (Exception ex) when (ex is FormatException || ex is JsonException)
                {
                    throw new InvalidOperationException(
                        "La variable FIREBASE_SERVICE_ACCOUNT_BASE64 no contiene una credencial válida.",
                        ex);
                }
            }

            var credentialPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Data",
                "serviceAccountKey.json");
            if (!File.Exists(credentialPath))
            {
                throw new FileNotFoundException(
                    "No se configuró la credencial de Firebase en la variable de entorno ni en el archivo local.",
                    credentialPath);
            }

            return GoogleCredential.FromFile(credentialPath);
        }

        internal static void SendNotification(object userEnviaFirebaseToken, object userEnviaEmail, object userEnviaFullName, string v, object value)
        {
            throw new NotImplementedException();
        }




        /*
        public static string SendPushNotification(string? deviceId,
                                                  string titulo,
                                                  string mensaje)
        {
            string response;

            try
            {
                                                                                                                                                                                              // topic notification
                WebRequest tRequest = WebRequest.Create("https://fcm.googleapis.com/fcm/send");

                tRequest.Method = "post";
                tRequest.ContentType = "application/json";
                var data = new
                {
                    to = deviceId,
                    notification = new
                    {
                        body = mensaje,
                        title = titulo,
                        sound = "Enabled"
                    }
                };

                //var serializer = new JavaScriptSerializer();
                var json = JsonSerializer.Serialize(data);
                Byte[] byteArray = Encoding.UTF8.GetBytes(json);
                tRequest.Headers.Add(string.Format("Authorization: key={0}", applicationID));
                tRequest.Headers.Add(string.Format("Sender: id={0}", senderId));
                tRequest.ContentLength = byteArray.Length;

                using (Stream dataStream = tRequest.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    using (WebResponse tResponse = tRequest.GetResponse())
                    {
                        using (Stream dataStreamResponse = tResponse.GetResponseStream())
                        {
                            using (StreamReader tReader = new StreamReader(dataStreamResponse))
                            {
                                String sResponseFromServer = tReader.ReadToEnd();
                                response = sResponseFromServer;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response = ex.Message;
            }

            return response;
        }
        */

    }
}



