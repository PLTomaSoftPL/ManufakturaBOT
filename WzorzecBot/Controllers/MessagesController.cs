using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Web.Http;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json.Linq;
using Parameters;
using GksKatowiceBot.Helpers;
using System.Json;
using Microsoft.Bot.Builder.Dialogs;

namespace GksKatowiceBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody] Activity activity)
        {
            try
            {
                if (activity.Type == ActivityTypes.Message)
                {

                    if (BaseDB.czyAdministrator(activity.From.Id) != null && (((activity.Text != null && activity.Text.IndexOf("!!!") == 0) || (activity.Attachments != null && activity.Attachments.Count > 0))))
                    {
                        WebClient client = new WebClient();

                        if (activity.Text != null && activity.Text.ToUpper().IndexOf("!!!ANKIETA") > -1)
                        {
                            try
                            {
                                //     int index = activity.Text.ToUpper().IndexOf("!!!ANKIETA");
                                DataTable dt = BaseDB.DajAnkiete(Convert.ToInt32(activity.Text.ToUpper().Substring(10)));
                                if (dt.Rows.Count > 0)
                                {
                                    CreateAnkieta(dt, activity.From.Id.ToString());
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        }

                        else if (activity.Attachments != null)
                        {
                            //Uri uri = new Uri(activity.Attachments[0].ContentUrl);
                            string filename = activity.Attachments[0].ContentUrl.Substring(activity.Attachments[0].ContentUrl.Length - 4, 3).Replace(".", "");


                            //  WebClient client = new WebClient();
                            client.Credentials = new NetworkCredential("serwer1606926", "Tomason1910");
                            client.BaseAddress = "ftp://serwer1606926.home.pl/public_html/pub/";


                            byte[] data;
                            using (WebClient client2 = new WebClient())
                            {
                                data = client2.DownloadData(activity.Attachments[0].ContentUrl);
                            }
                            if (activity.Attachments[0].ContentType.Contains("image")) client.UploadData(filename + ".png", data); //since the baseaddress
                            else if (activity.Attachments[0].ContentType.Contains("video")) client.UploadData(filename + ".mp4", data);
                        }


                        CreateMessage(activity.Attachments, activity.Text == null ? "" : activity.Text.Replace("!!!", ""), activity.From.Id);

                    }
                    else
                    {
                        string komenda = "";
                        if (activity.ChannelData != null)
                        {
                            try
                            {
                                BaseDB.AddToLog("Przesylany Json " + activity.ChannelData.ToString());
                                dynamic stuff = JsonConvert.DeserializeObject(activity.ChannelData.ToString());
                                komenda = stuff.message.quick_reply.payload;
                                BaseDB.AddToLog("Komenda: " + komenda);
                            }
                            catch (Exception ex)
                            {
                                BaseDB.AddToLog("Bład rozkładania Jsona " + ex.ToString());
                            }
                        }

                        MicrosoftAppCredentials.TrustServiceUrl(@"https://facebook.botframework.com", DateTime.MaxValue);


                        var toReply = activity.CreateReply(String.Empty);
                        var connectorNew = new ConnectorClient(new Uri(activity.ServiceUrl));
                        toReply.Type = ActivityTypes.Typing;
                        await connectorNew.Conversations.SendToConversationAsync(toReply);

                        if (komenda == "DEVELOPER_DEFINED_PAYLOAD_Aktualnosci" || activity.Text == "DEVELOPER_DEFINED_PAYLOAD_Aktualnosci")
                        {
                            List<IGrouping<string, string>> hrefList = new List<IGrouping<string, string>>();
                            var attechments = BaseGETMethod.GetCardsAttachmentsAktualnosci(ref hrefList, true);
                            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                            var userAccount = new ChannelAccount(name: activity.From.Name, id: activity.From.Id);
                            var botAccount = new ChannelAccount(name: activity.Recipient.Name, id: activity.Recipient.Id);
                            connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                            var conversationId = await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount);
                            IMessageActivity message = Activity.CreateMessageActivity();
                            message.ChannelData = JObject.FromObject(new
                            {
                                notification_type = "REGULAR",

                                quick_replies = new dynamic[]
                                   {
                                                                                                new
                                {
                                    content_type = "text",
                                    title = "Wydarzenia",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Aktualnosci",
                                    //     image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                 //   image_url = "http://archiwum.koluszki.pl/zdjecia/naglowki_nowe/listopad%202013/pi%C5%82ka[1].png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Nowości/Promocje",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Promocje",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                                                new
                                {
                                    content_type = "text",
                                    title = "Kultura i Sztuka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_KulturaSztuka",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Rozrywka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Rozrywka",
                                },
                              }
                            });




                            message.From = botAccount;
                            message.Recipient = userAccount;
                            message.Conversation = new ConversationAccount(id: conversationId.Id);
                            message.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                            message.Text = "Zobacz co nowego w Manufakturze :information_source:";
                            message.Attachments = attechments;

                            await connector.Conversations.SendToConversationAsync((Activity)message);
                        }
                        else if (komenda.Contains("DEVELOPER_DEFINED_PAYLOAD_Odpowiedz") || activity.Text.Contains("DEVELOPER_DEFINED_PAYLOAD_Odpowiedz"))
                        {

                            if (komenda.Substring(35, 1) == "1")
                            {

                                Parameters.Parameters.userDataStruct userStruct = new Parameters.Parameters.userDataStruct();
                                userStruct.userName = activity.From.Name;
                                userStruct.userId = activity.From.Id;
                                userStruct.botName = activity.Recipient.Name;
                                userStruct.botId = activity.Recipient.Id;
                                userStruct.ServiceUrl = activity.ServiceUrl;

                                BaseDB.zapiszOdpowiedzi(komenda.Substring(komenda.LastIndexOf('_') + 1), 1, 0, 0, 0, 0, 0);

                                Parameters.Parameters.listaAdresow.Add(userStruct);
                                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                                var userAccount = new ChannelAccount(name: activity.From.Name, id: activity.From.Id);
                                var botAccount = new ChannelAccount(name: activity.Recipient.Name, id: activity.Recipient.Id);
                                connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                                var conversationId = await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount);
                                IMessageActivity message = Activity.CreateMessageActivity();
                                message.ChannelData = JObject.FromObject(new
                                {
                                    notification_type = "REGULAR",


                                    buttons = new dynamic[]
                                {
                            new
                        {
                                type = "web_url",
                                url = "https://petersfancyapparel.com/classic_white_tshirt",
                                title = "Wyniki",
                                webview_height_ratio = "compact"
                            }
                                },

                                    quick_replies = new dynamic[]
                            {
                                //new
                                //{
                                //    content_type = "text",
                                //    title = "Aktualności",
                                //    payload = "DEFINED_PAYLOAD_FOR_PICKING_BLUE",
                                //    image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Blue%20Ball.png"
                                //},
                                new
                                {
                                    content_type = "text",
                                    title = "Dla mieszkańców",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_DlaMieszkancow",
                                    //     image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                            //        image_url = "http://archiwum.koluszki.pl/zdjecia/naglowki_nowe/listopad%202013/pi%C5%82ka[1].png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Dla turystów",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_DlaTurystow",
                       //             image_url = "https://gim7bytom.edupage.org/global/pics/iconspro/sport/volleyball.png"
                                },                                new
                                {
                                    content_type = "text",
                                    title = "Dla inwestorów",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_DlaInwestorow",
                                //       image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                },
                                                           }
                                });


                                message.From = botAccount;
                                message.Recipient = userAccount;
                                message.Conversation = new ConversationAccount(id: conversationId.Id);
                                message.Text = "Super dziękuję za oddanie głosu :)";
                                await connector.Conversations.SendToConversationAsync((Activity)message);
                            }
                        }
                        else if (komenda == "DEVELOPER_DEFINED_PAYLOAD_POWIADOMIENIA" || activity.Text == "DEVELOPER_DEFINED_PAYLOAD_POWIADOMIENIA" || activity.Text == "Powiadomienia")
                        {
                            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                            var userAccount = new ChannelAccount(name: activity.From.Name, id: activity.From.Id);
                            var botAccount = new ChannelAccount(name: activity.Recipient.Name, id: activity.Recipient.Id);
                            connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                            var conversationId = await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount);
                            IMessageActivity message = Activity.CreateMessageActivity();
                            byte czyPowiadomienia = BaseDB.czyPowiadomienia(userAccount.Id);
                            if (czyPowiadomienia == 0)
                            {
                                message.Text = "Opcja automatycznych, powiadomień o aktualnościach  jest włączona. Jeśli nie chcesz otrzymywać powiadomień  możesz je wyłączyć.";
                                message.ChannelData = JObject.FromObject(new
                                {
                                    notification_type = "REGULAR",
                                    //buttons = new dynamic[]
                                    // {
                                    //     new
                                    //     {
                                    //    type ="postback",
                                    //    title="Tytul",
                                    //    vslue = "tytul",
                                    //    payload="DEVELOPER_DEFINED_PAYLOAD"
                                    //     }
                                    // },
                                    quick_replies = new dynamic[]
                                           {
                                //new
                                //{
                                //    content_type = "text",
                                //    title = "Aktualności",
                                //    payload = "DEFINED_PAYLOAD_FOR_PICKING_BLUE",
                                //    image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Blue%20Ball.png"
                                //},
                                //new
                                //{
                                //    content_type = "text",
                                //    title = "Aktualności",
                                //    payload = "DEVELOPER_DEFINED_PAYLOAD_Aktualności",
                                ////       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                //},
                                new
                                {
                                    content_type = "text",
                                    title = "Wyłącz powiadomienia",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_PowiadomieniaWylacz",
                                    //     image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                 //   image_url = "http://archiwum.koluszki.pl/zdjecia/naglowki_nowe/listopad%202013/pi%C5%82ka[1].png"
                                },
                                               //new
                                               //{
                                               //    content_type = "text",
                                               //    title = "Włącz",
                                               //    payload = "DEVELOPER_DEFINED_PAYLOAD_PowiadomieniaWlacz",
                                               //   //   image_url = "https://gim7bytom.edupage.org/global/pics/iconspro/sport/volleyball.png"
                                               //},

                                           }
                                });
                            }
                            else if (czyPowiadomienia == 1)
                            {
                                message.Text = "Opcja automatycznych, codziennych  powiadomień o aktualnościach jest wyłączona. Jeśli chcesz otrzymywać powiadomienia możesz je włączyć.";
                                message.ChannelData = JObject.FromObject(new
                                {
                                    notification_type = "REGULAR",
                                    //buttons = new dynamic[]
                                    // {
                                    //     new
                                    //     {
                                    //    type ="postback",
                                    //    title="Tytul",
                                    //    vslue = "tytul",
                                    //    payload="DEVELOPER_DEFINED_PAYLOAD"
                                    //     }
                                    // },
                                    quick_replies = new dynamic[]
           {
                                //new
                                //{
                                //    content_type = "text",
                                //    title = "Aktualności",
                                //    payload = "DEFINED_PAYLOAD_FOR_PICKING_BLUE",
                                //    image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Blue%20Ball.png"
                                //},
                                //new
                                //{
                                //    content_type = "text",
                                //    title = "Aktualności",
                                //    payload = "DEVELOPER_DEFINED_PAYLOAD_Aktualności",
                                ////       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                //},
                                //new
                                //{
                                //    content_type = "text",
                                //    title = "Wyłącz",
                                //    payload = "DEVELOPER_DEFINED_PAYLOAD_PowiadomieniaWylacz",
                                //    //     image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                // //   image_url = "http://archiwum.koluszki.pl/zdjecia/naglowki_nowe/listopad%202013/pi%C5%82ka[1].png"
                                //},
                                new
                                {
                                    content_type = "text",
                                    title = "Włącz powiadomienia",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_PowiadomieniaWlacz",
                                   //   image_url = "https://gim7bytom.edupage.org/global/pics/iconspro/sport/volleyball.png"
                                },

           }
                                });
                            }
                            message.From = botAccount;
                            message.Recipient = userAccount;
                            message.Conversation = new ConversationAccount(id: conversationId.Id);
                            message.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                            List<IGrouping<string, string>> hrefList = new List<IGrouping<string, string>>();
                            //    message.Attachments = BaseGETMethod.GetCardsAttachmentsNajnowsze(ref hrefList, true);
                            //     message.Text = "W kazdej chwili możesz włączyć lub wyłączyć otrzymywanie powiadomień na swojego Messengera. Co chcesz zrobić z powiadomieniami? ";
                            await connector.Conversations.SendToConversationAsync((Activity)message);
                        }

                        else if (komenda == "DEVELOPER_DEFINED_PAYLOAD_PowiadomieniaWylacz" || activity.Text == "DEVELOPER_DEFINED_PAYLOAD_PowiadomieniaWylacz" || activity.Text == "Wyłącz")
                        {
                            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                            var userAccount = new ChannelAccount(name: activity.From.Name, id: activity.From.Id);
                            var botAccount = new ChannelAccount(name: activity.Recipient.Name, id: activity.Recipient.Id);
                            connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                            var conversationId = await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount);
                            IMessageActivity message = Activity.CreateMessageActivity();

                            message.ChannelData = JObject.FromObject(new
                            {
                                notification_type = "REGULAR",
                                //buttons = new dynamic[]
                                // {
                                //     new
                                //     {
                                //    type ="postback",
                                //    title="Tytul",
                                //    vslue = "tytul",
                                //    payload="DEVELOPER_DEFINED_PAYLOAD"
                                //     }
                                // },
                                quick_replies = new dynamic[]
                                   {
                                                                                                new
                                {
                                    content_type = "text",
                                    title = "Wydarzenia",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Aktualnosci",
                                    //     image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                 //   image_url = "http://archiwum.koluszki.pl/zdjecia/naglowki_nowe/listopad%202013/pi%C5%82ka[1].png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Nowości/Promocje",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Promocje",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                                                new
                                {
                                    content_type = "text",
                                    title = "Kultura i Sztuka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_KulturaSztuka",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Rozrywka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Rozrywka",
                                },
                              }
                            });
                            message.From = botAccount;
                            message.Recipient = userAccount;
                            message.Conversation = new ConversationAccount(id: conversationId.Id);
                            message.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                            List<IGrouping<string, string>> hrefList = new List<IGrouping<string, string>>();
                            //  message.Attachments = BaseGETMethod.GetCardsAttachmentsNajnowsze(ref hrefList, true);
                            message.Text = "Zrozumiałem, wyłączyłem automatyczne, codzienne powiadomienia o aktualnościach.";
                            BaseDB.ChangeNotification(Convert.ToInt64(userAccount.Id), 1);
                            await connector.Conversations.SendToConversationAsync((Activity)message);
                        }
                        else if (komenda == "DEVELOPER_DEFINED_PAYLOAD_PowiadomieniaWlacz" || activity.Text == "DEVELOPER_DEFINED_PAYLOAD_PowiadomieniaWlacz" || activity.Text == "Wyłącz")
                        {
                            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                            var userAccount = new ChannelAccount(name: activity.From.Name, id: activity.From.Id);
                            var botAccount = new ChannelAccount(name: activity.Recipient.Name, id: activity.Recipient.Id);
                            connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                            var conversationId = await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount);
                            IMessageActivity message = Activity.CreateMessageActivity();

                            message.ChannelData = JObject.FromObject(new
                            {
                                notification_type = "REGULAR",
                                //buttons = new dynamic[]
                                // {
                                //     new
                                //     {
                                //    type ="postback",
                                //    title="Tytul",
                                //    vslue = "tytul",
                                //    payload="DEVELOPER_DEFINED_PAYLOAD"
                                //     }
                                // },
                                quick_replies = new dynamic[]
                                   {
                                                                                                new
                                {
                                    content_type = "text",
                                    title = "Wydarzenia",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Aktualnosci",
                                    //     image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                 //   image_url = "http://archiwum.koluszki.pl/zdjecia/naglowki_nowe/listopad%202013/pi%C5%82ka[1].png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Nowości/Promocje",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Promocje",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                                                new
                                {
                                    content_type = "text",
                                    title = "Kultura i Sztuka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_KulturaSztuka",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Rozrywka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Rozrywka",
                                },
                              }
                            });
                            message.From = botAccount;
                            message.Recipient = userAccount;
                            message.Conversation = new ConversationAccount(id: conversationId.Id);
                            message.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                            List<IGrouping<string, string>> hrefList = new List<IGrouping<string, string>>();
                            //  message.Attachments = BaseGETMethod.GetCardsAttachmentsNajnowsze(ref hrefList, true);
                            message.Text = "Zrozumiałem, włączyłem automatyczne, codzienne powiadomienia o aktualnościach.";
                            BaseDB.ChangeNotification(Convert.ToInt64(userAccount.Id), 0);
                            await connector.Conversations.SendToConversationAsync((Activity)message);
                        }

                        else if (komenda == "<GET_STARTED_PAYLOAD>" || activity.Text == "<GET_STARTED_PAYLOAD>" || activity.Text == "Rozpocznij" || activity.Text == "Get Started")
                        {
                            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                            var userAccount = new ChannelAccount(name: activity.From.Name, id: activity.From.Id);
                            var botAccount = new ChannelAccount(name: activity.Recipient.Name, id: activity.Recipient.Id);
                            connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                            var conversationId = await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount);
                            IMessageActivity message = Activity.CreateMessageActivity();
                            message.ChannelData = JObject.FromObject(new
                            {
                                notification_type = "REGULAR",

                                quick_replies = new dynamic[]
                                   {
                                                                                                new
                                {
                                    content_type = "text",
                                    title = "Wydarzenia",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Aktualnosci",
                                    //     image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                 //   image_url = "http://archiwum.koluszki.pl/zdjecia/naglowki_nowe/listopad%202013/pi%C5%82ka[1].png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Nowości/Promocje",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Promocje",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                                                new
                                {
                                    content_type = "text",
                                    title = "Kultura i Sztuka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_KulturaSztuka",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Rozrywka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Rozrywka",
                                },
                              }
                            });



                            message.From = botAccount;
                            message.Recipient = userAccount;
                            message.Conversation = new ConversationAccount(id: conversationId.Id);
                            message.AttachmentLayout = AttachmentLayoutTypes.Carousel;


                            message.Text = "Cześć  " + userAccount.Name.Substring(0, userAccount.Name.IndexOf(" ")) + " , jestem wirtualnym asystenetem do kontaktu z galerią Manufaktura :sunglasses: Z moją pomocą dowiesz się o wszystkim co najważniejsze w naszej galerii, raz w tygodniu mogę powiadomić Cie o wydarzeniach i promocjach. Postaram się również udzielić odpowiedzi na Twoje pytania :) ";
                            BaseDB.AddUser(userAccount.Name, userAccount.Id, botAccount.Name, botAccount.Id, "https://facebook.botframework.com", 0);
                            await connector.Conversations.SendToConversationAsync((Activity)message);
                        }
                        else if (komenda == "DEVELOPER_DEFINED_PAYLOAD_Promocje" || activity.Text == "DEVELOPER_DEFINED_PAYLOAD_Promocje")
                        {
                            List<IGrouping<string, string>> hrefList = new List<IGrouping<string, string>>();
                            var attechments = BaseGETMethod.GetCardsAttachmentsPromocje(ref hrefList, true);
                            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                            var userAccount = new ChannelAccount(name: activity.From.Name, id: activity.From.Id);
                            var botAccount = new ChannelAccount(name: activity.Recipient.Name, id: activity.Recipient.Id);
                            connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                            var conversationId = await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount);
                            IMessageActivity message = Activity.CreateMessageActivity();
                            message.ChannelData = JObject.FromObject(new
                            {
                                notification_type = "REGULAR",

                                quick_replies = new dynamic[]
                                   {
                                                                                                new
                                {
                                    content_type = "text",
                                    title = "Wydarzenia",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Aktualnosci",
                                    //     image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                 //   image_url = "http://archiwum.koluszki.pl/zdjecia/naglowki_nowe/listopad%202013/pi%C5%82ka[1].png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Nowości/Promocje",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Promocje",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                                                new
                                {
                                    content_type = "text",
                                    title = "Kultura i Sztuka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_KulturaSztuka",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Rozrywka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Rozrywka",
                                },
                              }
                            });

                            message.From = botAccount;
                            message.Recipient = userAccount;
                            message.Conversation = new ConversationAccount(id: conversationId.Id);
                            message.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                            message.Text = "Sprawdź promocje w naszych sklepach :sunglasses:";
                            message.Attachments = attechments;

                            await connector.Conversations.SendToConversationAsync((Activity)message);
                        }
                        else if (komenda == "DEVELOPER_DEFINED_PAYLOAD_KulturaSztuka" || activity.Text == "DEVELOPER_DEFINED_PAYLOAD_KulturaSztuka")
                        {
                            List<IGrouping<string, string>> hrefList = new List<IGrouping<string, string>>();
                            var attechments = BaseGETMethod.GetCardsAttachmentsKulturaSztuka(ref hrefList, true);
                            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                            var userAccount = new ChannelAccount(name: activity.From.Name, id: activity.From.Id);
                            var botAccount = new ChannelAccount(name: activity.Recipient.Name, id: activity.Recipient.Id);
                            connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                            var conversationId = await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount);
                            IMessageActivity message = Activity.CreateMessageActivity();
                            message.ChannelData = JObject.FromObject(new
                            {
                                notification_type = "REGULAR",

                                quick_replies = new dynamic[]
                                   {
                                                                                                new
                                {
                                    content_type = "text",
                                    title = "Wydarzenia",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Aktualnosci",
                                    //     image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                 //   image_url = "http://archiwum.koluszki.pl/zdjecia/naglowki_nowe/listopad%202013/pi%C5%82ka[1].png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Nowości/Promocje",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Promocje",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                                                new
                                {
                                    content_type = "text",
                                    title = "Kultura i Sztuka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_KulturaSztuka",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Rozrywka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Rozrywka",
                                },
                              }
                            });
                            message.From = botAccount;
                            message.Recipient = userAccount;
                            message.Conversation = new ConversationAccount(id: conversationId.Id);
                            message.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                            message.Text = "Sprawdź co nowego w kulturze i sztuce :)";
                            message.Attachments = attechments;

                            await connector.Conversations.SendToConversationAsync((Activity)message);
                        }
                        else if (komenda == "DEVELOPER_DEFINED_PAYLOAD_Rozrywka" || activity.Text == "DEVELOPER_DEFINED_PAYLOAD_Rozrywka")
                        {
                            List<IGrouping<string, string>> hrefList = new List<IGrouping<string, string>>();
                            var attechments = BaseGETMethod.GetCardsAttachmentsRozrywka(ref hrefList, true);
                            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                            var userAccount = new ChannelAccount(name: activity.From.Name, id: activity.From.Id);
                            var botAccount = new ChannelAccount(name: activity.Recipient.Name, id: activity.Recipient.Id);
                            connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                            var conversationId = await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount);
                            IMessageActivity message = Activity.CreateMessageActivity();
                            message.ChannelData = JObject.FromObject(new
                            {
                                notification_type = "REGULAR",

                                quick_replies = new dynamic[]
                                   {
                                                                                                new
                                {
                                    content_type = "text",
                                    title = "Wydarzenia",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Aktualnosci",
                                    //     image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                 //   image_url = "http://archiwum.koluszki.pl/zdjecia/naglowki_nowe/listopad%202013/pi%C5%82ka[1].png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Nowości/Promocje",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Promocje",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                                                new
                                {
                                    content_type = "text",
                                    title = "Kultura i Sztuka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_KulturaSztuka",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Rozrywka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Rozrywka",
                                },
                              }
                            });

                            message.From = botAccount;
                            message.Recipient = userAccount;
                            message.Conversation = new ConversationAccount(id: conversationId.Id);
                            message.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                            message.Text = "Manufaktura - miejsce pełne rozrywki :heartpulse:";
                            message.Attachments = attechments;

                            await connector.Conversations.SendToConversationAsync((Activity)message);
                        }
                        else
                        {

                            List<IGrouping<string, string>> hrefList = new List<IGrouping<string, string>>();
                            var sklepy = BaseGETMethod.GetCardsAttachmentsSklepy(ref hrefList, false, activity.Text);
                            if (sklepy.Count() > 0)
                            {
                                try
                                {
                                    ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                                    var userAccount = new ChannelAccount(name: activity.From.Name, id: activity.From.Id);
                                    var botAccount = new ChannelAccount(name: activity.Recipient.Name, id: activity.Recipient.Id);
                                    connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                                    var conversationId = await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount);
                                    IMessageActivity message = Activity.CreateMessageActivity();
                                    message.ChannelData = JObject.FromObject(new
                                    {
                                        notification_type = "REGULAR",

                                        quick_replies = new dynamic[]
                                   {
                                                                                                new
                                {
                                    content_type = "text",
                                    title = "Wydarzenia",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Aktualnosci",
                                    //     image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                 //   image_url = "http://archiwum.koluszki.pl/zdjecia/naglowki_nowe/listopad%202013/pi%C5%82ka[1].png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Nowości/Promocje",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Promocje",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                                                new
                                {
                                    content_type = "text",
                                    title = "Kultura i Sztuka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_KulturaSztuka",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Rozrywka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Rozrywka",
                                },
                              }
                                    });


                                    message.From = botAccount;
                                    message.Recipient = userAccount;
                                    message.Conversation = new ConversationAccount(id: conversationId.Id);
                                    message.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                    message.Text = "W sprawach związanych z konkretnym sklepem prosimy o sprawdzenie na stronie sklepu lub kontakt telefoniczny ze sklepem. Wszelkie informacje znajdziesz na stronach poniżej";
                                    message.Attachments = sklepy;
                                    // message.Text = "Niestety nie znalazłem informacji na ten temat. Skorzystaj z podpowiedzi.";

                                    await connector.Conversations.SendToConversationAsync((Activity)message);

                                }
                                catch (Exception ex)
                                {
                                    BaseDB.AddToLog("Błąd wysywania informacji o sklepie: " + ex);
                                }
                            }
                            else
                            {
                                var szablony = BaseGETMethod.GetCardsAttachmentsSprawdzSzablon(ref hrefList, false, activity.Text);
                                if (szablony.Rows.Count > 0)
                                {


                                    if (szablony.Rows.Count == 1)
                                    {
                                        try
                                        {
                                            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                                            var userAccount = new ChannelAccount(name: activity.From.Name, id: activity.From.Id);
                                            var botAccount = new ChannelAccount(name: activity.Recipient.Name, id: activity.Recipient.Id);
                                            connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                                            var conversationId = await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount);
                                            IMessageActivity message = Activity.CreateMessageActivity();
                                            message.ChannelData = JObject.FromObject(new
                                            {
                                                notification_type = "REGULAR",

                                                quick_replies = new dynamic[]
                                   {
                                                                                                new
                                {
                                    content_type = "text",
                                    title = "Wydarzenia",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Aktualnosci",
                                    //     image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                 //   image_url = "http://archiwum.koluszki.pl/zdjecia/naglowki_nowe/listopad%202013/pi%C5%82ka[1].png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Nowości/Promocje",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Promocje",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                                                new
                                {
                                    content_type = "text",
                                    title = "Kultura i Sztuka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_KulturaSztuka",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Rozrywka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Rozrywka",
                                },
                              }
                                            });


                                            message.From = botAccount;
                                            message.Recipient = userAccount;
                                            message.Conversation = new ConversationAccount(id: conversationId.Id);
                                            message.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                            message.Text = szablony.Rows[0]["Odpowiedz"].ToString();


                                            await connector.Conversations.SendToConversationAsync((Activity)message);

                                        }
                                        catch (Exception ex)
                                        {
                                            BaseDB.AddToLog("Błąd wysywania informacji o sklepie: " + ex);
                                        }
                                    }

                                    else
                                    {
                                        try
                                        {
                                            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                                            var userAccount = new ChannelAccount(name: activity.From.Name, id: activity.From.Id);
                                            var botAccount = new ChannelAccount(name: activity.Recipient.Name, id: activity.Recipient.Id);
                                            connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                                            var conversationId = await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount);
                                            IMessageActivity message = Activity.CreateMessageActivity();
                                            message.ChannelData = JObject.FromObject(new
                                            {
                                                notification_type = "REGULAR",

                                                quick_replies = new dynamic[]
                                   {
                                                                                                new
                                {
                                    content_type = "text",
                                    title = "Wydarzenia",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Aktualnosci",
                                    //     image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                 //   image_url = "http://archiwum.koluszki.pl/zdjecia/naglowki_nowe/listopad%202013/pi%C5%82ka[1].png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Nowości/Promocje",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Promocje",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                                                new
                                {
                                    content_type = "text",
                                    title = "Kultura i Sztuka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_KulturaSztuka",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Rozrywka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Rozrywka",
                                },
                              }
                                            });
                                            message.From = botAccount;
                                            message.Recipient = userAccount;
                                            message.Conversation = new ConversationAccount(id: conversationId.Id);
                                            message.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                            message.Text = "Znalazłem więcej niż jedną odpowiedź pasującą do Twojego pytania. Sprawdź w podpowiedziach czy jest odpowiedź na Twoje pytanie lub wyślij pytanie do Nas. Odpowiem najszybciej jak to możliwe :)";

                                            await connector.Conversations.SendToConversationAsync((Activity)message);

                                        }
                                        catch (Exception ex)
                                        {
                                            BaseDB.AddToLog("Błąd wysywania informacji o sklepie: " + ex);
                                        }
                                    }
                                }
                                else
                                {
                                    var wyszukiwarka = BaseGETMethod.GetCardsAttachmentsWyszukiwarka(ref hrefList, false, activity.Text);
                                    if (wyszukiwarka.Count() > 0)
                                    {
                                        ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                                        var userAccount = new ChannelAccount(name: activity.From.Name, id: activity.From.Id);
                                        var botAccount = new ChannelAccount(name: activity.Recipient.Name, id: activity.Recipient.Id);
                                        connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                                        var conversationId = await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount);
                                        IMessageActivity message = Activity.CreateMessageActivity();
                                        message.ChannelData = JObject.FromObject(new
                                        {
                                            notification_type = "REGULAR",

                                            quick_replies = new dynamic[]
                                   {
                                                                                                new
                                {
                                    content_type = "text",
                                    title = "Wydarzenia",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Aktualnosci",
                                    //     image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                 //   image_url = "http://archiwum.koluszki.pl/zdjecia/naglowki_nowe/listopad%202013/pi%C5%82ka[1].png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Nowości/Promocje",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Promocje",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                                                new
                                {
                                    content_type = "text",
                                    title = "Kultura i Sztuka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_KulturaSztuka",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Rozrywka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Rozrywka",
                                },
                              }
                                        });

                                        message.From = botAccount;
                                        message.Recipient = userAccount;
                                        message.Conversation = new ConversationAccount(id: conversationId.Id);
                                        message.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                                        message.Attachments = wyszukiwarka;
                                        // message.Text = "Niestety nie znalazłem informacji na ten temat. Skorzystaj z podpowiedzi.";

                                        await connector.Conversations.SendToConversationAsync((Activity)message);
                                    }
                                    else
                                    {

                                        ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                                        var userAccount = new ChannelAccount(name: activity.From.Name, id: activity.From.Id);
                                        var botAccount = new ChannelAccount(name: activity.Recipient.Name, id: activity.Recipient.Id);
                                        BaseDB.dodajNowaWiadomosc(activity.Text, userAccount.Id, userAccount.Name);
                                        connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                                        var conversationId = await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount);
                                        IMessageActivity message = Activity.CreateMessageActivity();
                                        message.ChannelData = JObject.FromObject(new
                                        {
                                            notification_type = "REGULAR",

                                            quick_replies = new dynamic[]
                                   {
                                                                                                new
                                {
                                    content_type = "text",
                                    title = "Wydarzenia",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Aktualnosci",
                                    //     image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                 //   image_url = "http://archiwum.koluszki.pl/zdjecia/naglowki_nowe/listopad%202013/pi%C5%82ka[1].png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Nowości/Promocje",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Promocje",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                                                new
                                {
                                    content_type = "text",
                                    title = "Kultura i Sztuka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_KulturaSztuka",
                                //       image_url = "https://www.samo-lepky.sk/data/11/hokej5.png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Rozrywka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Rozrywka",
                                },
                              }
                                        });

                                        message.From = botAccount;
                                        message.Recipient = userAccount;
                                        message.Conversation = new ConversationAccount(id: conversationId.Id);
                                        message.AttachmentLayout = AttachmentLayoutTypes.Carousel;



                                        message.Text = "Niestety nie znalazłem informacji na ten temat. Twoja wiadomość została przekazana do konsultatna.W niedługim czasie spodziewaj się opowiedzi. Tymaczasem możesz skorzystać z naszych propozycji.";

                                        await connector.Conversations.SendToConversationAsync((Activity)message);
                                    }
                                }
                            }

                        }
                        
                    }
                }
                else
                {
                    HandleSystemMessage(activity);
                }
            }
            catch (Exception ex)
            {
                BaseDB.AddToLog("Wysylanie wiadomosci: " + ex.InnerException.ToString());
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        public async static void CreateMessage(IList<Attachment> foto, string wiadomosc, string fromId)
        {
            try
            {
                BaseDB.AddToLog("Wywołanie metody CreateMessage");

                string uzytkownik = "";
                DataTable dt = BaseGETMethod.GetUser();

                try
                {
                    MicrosoftAppCredentials.TrustServiceUrl(@"https://facebook.botframework.com", DateTime.MaxValue);

                    IMessageActivity message = Activity.CreateMessageActivity();
                    message.ChannelData = JObject.FromObject(new
                    {
                        notification_type = "REGULAR",
                        quick_replies = new dynamic[]
                            {
                               new
                        {
                                    content_type = "text",
                                    title = "Piłka nożna",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Pilka_Nozna",
                                  //  image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                },
                                new
                        {
                                    content_type = "text",
                                    title = "Siatkówka",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Siatkowka",
                                   // image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Blue%20Ball.png"
                                },                                new
                        {
                                    content_type = "text",
                                    title = "Hokej",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Hokej",
                                   // image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                },
                                                           }
                    });

                    message.AttachmentLayout = null;

                    if (foto != null && foto.Count > 0)
                    {
                        string filename = foto[0].ContentUrl.Substring(foto[0].ContentUrl.Length - 4, 3).Replace(".", "");

                        if (foto[0].ContentType.Contains("image")) foto[0].ContentUrl = "http://serwer1606926.home.pl/pub/" + filename + ".png";//since the baseaddress
                        else if (foto[0].ContentType.Contains("video")) foto[0].ContentUrl = "http://serwer1606926.home.pl/pub/" + filename + ".mp4";

                        //foto[0].ContentUrl = "http://serwer1606926.home.pl/pub/" + filename + ".png";

                        message.Attachments = foto;
                    }


                    //var list = new List<Attachment>();
                    //if (foto != null)
                    //{
                    //    for (int i = 0; i < foto.Count; i++)
                    //    {
                    //        list.Add(GetHeroCard(
                    //       foto[i].ContentUrl, "", "",
                    //       new CardImage(url: foto[i].ContentUrl),
                    //       new CardAction(ActionTypes.OpenUrl, "", value: ""),
                    //       new CardAction(ActionTypes.OpenUrl, "", value: "https://www.facebook.com/sharer/sharer.php?u=" + "")));
                    //    }
                    //}

                    message.Text = wiadomosc;
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        try
                        {
                            if (fromId != dt.Rows[i]["UserId"].ToString())
                            {

                                var userAccount = new ChannelAccount(name: dt.Rows[i]["UserName"].ToString(), id: dt.Rows[i]["UserId"].ToString());
                                uzytkownik = userAccount.Name;
                                var botAccount = new ChannelAccount(name: dt.Rows[i]["BotName"].ToString(), id: dt.Rows[i]["BotId"].ToString());
                                var connector = new ConnectorClient(new Uri(dt.Rows[i]["Url"].ToString()), "d2483171-4038-4fbe-b7a1-7d73bff7d046", "cUKwH06PFdwmLQoqpGYQLdJ");
                                var conversationId = await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount);
                                message.From = botAccount;
                                message.Recipient = userAccount;
                                message.Conversation = new ConversationAccount(id: conversationId.Id, isGroup: false);
                                //await connector.Conversations.SendToConversationAsync((Activity)message).ConfigureAwait(false);

                                var returne = await connector.Conversations.SendToConversationAsync((Activity)message);
                            }
                        }
                        catch (Exception ex)
                        {
                            BaseDB.AddToLog("Błąd wysyłania wiadomości do: " + uzytkownik + " " + ex.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    BaseDB.AddToLog("Błąd wysyłania wiadomości do: " + uzytkownik + " " + ex.ToString());
                }
            }
            catch (Exception ex)
            {
                BaseDB.AddToLog("Błąd wysłania wiadomosci: " + ex.ToString());
            }
        }






        public static void CallToChildThread()
        {
            try
            {
                Thread.Sleep(5000);
            }

            catch (ThreadAbortException e)
            {
                Console.WriteLine("Thread Abort Exception");
            }
            finally
            {
                Console.WriteLine("Couldn't catch the Thread Exception");
            }
        }




        public async static void CreateAnkieta(DataTable dtAnkieta, string fromId)
        {
            try
            {
                BaseDB.AddToLog("Wywołanie metody CreateAnkieta");

                string uzytkownik = "";
                DataTable dt = BaseGETMethod.GetUser();

                MicrosoftAppCredentials.TrustServiceUrl(@"https://facebook.botframework.com", DateTime.MaxValue);

                IMessageActivity message = Activity.CreateMessageActivity();

                try
                {
                    if (dtAnkieta.Rows.Count >= 3)
                    {

                        message.ChannelData = JObject.FromObject(new
                        {
                            notification_type = "REGULAR",

                            quick_replies = new dynamic[]
      {

                                new
                                {
                                    content_type = "text",
                                    title = dtAnkieta.Rows[0]["Odpowiedz1"].ToString(),
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Odpowiedz1_"+dtAnkieta.Rows[0]["ID"],
                                    //     image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                   // image_url = "http://archiwum.koluszki.pl/zdjecia/naglowki_nowe/listopad%202013/pi%C5%82ka[1].png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = dtAnkieta.Rows[0]["Odpowiedz2"].ToString(),
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Odpowiedz2_"+dtAnkieta.Rows[0]["ID"],
                           //         image_url = "https://gim7bytom.edupage.org/global/pics/iconspro/sport/volleyball.png"
                                },                                new
                                {
                                    content_type = "text",
                                    title = dtAnkieta.Rows[0]["Odpowiedz3"].ToString(),
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Odpowiedz3_"+dtAnkieta.Rows[0]["ID"],
                                //       image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                },



                                     }
                        });

                        message.AttachmentLayout = null;

                        message.Text = dtAnkieta.Rows[0]["Tresc"].ToString();
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            try
                            {
                                if (fromId != dt.Rows[i]["UserId"].ToString())
                                {

                                    var userAccount = new ChannelAccount(name: dt.Rows[i]["UserName"].ToString(), id: dt.Rows[i]["UserId"].ToString());
                                    uzytkownik = userAccount.Name;
                                    var botAccount = new ChannelAccount(name: dt.Rows[i]["BotName"].ToString(), id: dt.Rows[i]["BotId"].ToString());
                                    var connector = new ConnectorClient(new Uri(dt.Rows[i]["Url"].ToString()), "39d46c3f-67d1-4cd1-b5b6-89ec124abf63", "6AnFsopfzqcb4reBLJpKMUB");
                                    var conversationId = await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount);
                                    message.From = botAccount;
                                    message.Recipient = userAccount;
                                    message.Conversation = new ConversationAccount(id: conversationId.Id, isGroup: false);
                                    //await connector.Conversations.SendToConversationAsync((Activity)message).ConfigureAwait(false);

                                    var returne = await connector.Conversations.SendToConversationAsync((Activity)message);
                                }
                            }
                            catch (Exception ex)
                            {
                                BaseDB.AddToLog("Błąd wysyłania wiadomości do: " + uzytkownik + " " + ex.ToString());
                            }
                        }
                    }
                    else if (dtAnkieta.Rows.Count == 4)
                    {

                        message.ChannelData = JObject.FromObject(new
                        {
                            notification_type = "REGULAR",

                            quick_replies = new dynamic[]
      {

                                new
                                {
                                    content_type = "text",
                                    title = dtAnkieta.Rows[0]["Odpowiedz1"].ToString(),
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Odpowiedz1_"+dtAnkieta.Rows[0]["ID"],
                                    //     image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                   // image_url = "http://archiwum.koluszki.pl/zdjecia/naglowki_nowe/listopad%202013/pi%C5%82ka[1].png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = dtAnkieta.Rows[0]["Odpowiedz2"].ToString(),
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Odpowiedz2_"+dtAnkieta.Rows[0]["ID"],
                           //         image_url = "https://gim7bytom.edupage.org/global/pics/iconspro/sport/volleyball.png"
                                },                                new
                                {
                                    content_type = "text",
                                    title = dtAnkieta.Rows[0]["Odpowiedz3"].ToString(),
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Odpowiedz3_"+dtAnkieta.Rows[0]["ID"],
                                //       image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                },

                                                               new
                                {
                                    content_type = "text",
                                    title = dtAnkieta.Rows[0]["Odpowiedz4"].ToString(),
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Odpowiedz4_"+dtAnkieta.Rows[0]["ID"],
                                //       image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                },

                                     }
                        });

                        message.AttachmentLayout = null;

                        message.Text = dtAnkieta.Rows[0]["Tresc"].ToString();
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            try
                            {
                                if (fromId != dt.Rows[i]["UserId"].ToString())
                                {

                                    var userAccount = new ChannelAccount(name: dt.Rows[i]["UserName"].ToString(), id: dt.Rows[i]["UserId"].ToString());
                                    uzytkownik = userAccount.Name;
                                    var botAccount = new ChannelAccount(name: dt.Rows[i]["BotName"].ToString(), id: dt.Rows[i]["BotId"].ToString());
                                    var connector = new ConnectorClient(new Uri(dt.Rows[i]["Url"].ToString()), "39d46c3f-67d1-4cd1-b5b6-89ec124abf63", "6AnFsopfzqcb4reBLJpKMUB");
                                    var conversationId = await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount);
                                    message.From = botAccount;
                                    message.Recipient = userAccount;
                                    message.Conversation = new ConversationAccount(id: conversationId.Id, isGroup: false);
                                    //await connector.Conversations.SendToConversationAsync((Activity)message).ConfigureAwait(false);

                                    var returne = await connector.Conversations.SendToConversationAsync((Activity)message);
                                }
                            }
                            catch (Exception ex)
                            {
                                BaseDB.AddToLog("Błąd wysyłania wiadomości do: " + uzytkownik + " " + ex.ToString());
                            }
                        }
                    }
                    else if (dtAnkieta.Rows.Count == 5)
                    {
                        message.ChannelData = JObject.FromObject(new
                        {
                            notification_type = "REGULAR",

                            quick_replies = new dynamic[]
      {

                                new
                                {
                                    content_type = "text",
                                    title = dtAnkieta.Rows[0]["Odpowiedz1"].ToString(),
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Odpowiedz1_"+dtAnkieta.Rows[0]["ID"],
                                    //     image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                   // image_url = "http://archiwum.koluszki.pl/zdjecia/naglowki_nowe/listopad%202013/pi%C5%82ka[1].png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = dtAnkieta.Rows[0]["Odpowiedz2"].ToString(),
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Odpowiedz2_"+dtAnkieta.Rows[0]["ID"],
                           //         image_url = "https://gim7bytom.edupage.org/global/pics/iconspro/sport/volleyball.png"
                                },                                new
                                {
                                    content_type = "text",
                                    title = dtAnkieta.Rows[0]["Odpowiedz3"].ToString(),
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Odpowiedz3_"+dtAnkieta.Rows[0]["ID"],
                                //       image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                },

                                                               new
                                {
                                    content_type = "text",
                                    title = dtAnkieta.Rows[0]["Odpowiedz4"].ToString(),
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Odpowiedz4_"+dtAnkieta.Rows[0]["ID"],
                                //       image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                },
                                                                                            new
                                {
                                    content_type = "text",
                                    title = dtAnkieta.Rows[0]["Odpowiedz5"].ToString(),
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Odpowiedz5_"+dtAnkieta.Rows[0]["ID"],
                                //       image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                },

                                     }
                        });

                        message.AttachmentLayout = null;

                        message.Text = dtAnkieta.Rows[0]["Tresc"].ToString();
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            try
                            {
                                if (fromId != dt.Rows[i]["UserId"].ToString())
                                {

                                    var userAccount = new ChannelAccount(name: dt.Rows[i]["UserName"].ToString(), id: dt.Rows[i]["UserId"].ToString());
                                    uzytkownik = userAccount.Name;
                                    var botAccount = new ChannelAccount(name: dt.Rows[i]["BotName"].ToString(), id: dt.Rows[i]["BotId"].ToString());
                                    var connector = new ConnectorClient(new Uri(dt.Rows[i]["Url"].ToString()), "39d46c3f-67d1-4cd1-b5b6-89ec124abf63", "6AnFsopfzqcb4reBLJpKMUB");
                                    var conversationId = await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount);
                                    message.From = botAccount;
                                    message.Recipient = userAccount;
                                    message.Conversation = new ConversationAccount(id: conversationId.Id, isGroup: false);
                                    //await connector.Conversations.SendToConversationAsync((Activity)message).ConfigureAwait(false);

                                    var returne = await connector.Conversations.SendToConversationAsync((Activity)message);
                                }
                            }
                            catch (Exception ex)
                            {
                                BaseDB.AddToLog("Błąd wysyłania wiadomości do: " + uzytkownik + " " + ex.ToString());
                            }
                        }
                    }
                    else
                    {
                        message.ChannelData = JObject.FromObject(new
                        {
                            notification_type = "REGULAR",

                            quick_replies = new dynamic[]
      {

                                new
                                {
                                    content_type = "text",
                                    title = dtAnkieta.Rows[0]["Odpowiedz1"].ToString(),
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Odpowiedz1_"+dtAnkieta.Rows[0]["ID"],
                                    //     image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                   // image_url = "http://archiwum.koluszki.pl/zdjecia/naglowki_nowe/listopad%202013/pi%C5%82ka[1].png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = dtAnkieta.Rows[0]["Odpowiedz2"].ToString(),
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Odpowiedz2_"+dtAnkieta.Rows[0]["ID"],
                           //         image_url = "https://gim7bytom.edupage.org/global/pics/iconspro/sport/volleyball.png"
                                },                                new
                                {
                                    content_type = "text",
                                    title = dtAnkieta.Rows[0]["Odpowiedz3"].ToString(),
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Odpowiedz3_"+dtAnkieta.Rows[0]["ID"],
                                //       image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                },

                                                               new
                                {
                                    content_type = "text",
                                    title = dtAnkieta.Rows[0]["Odpowiedz4"].ToString(),
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Odpowiedz4_"+dtAnkieta.Rows[0]["ID"],
                                //       image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                },
                                                                                            new
                                {
                                    content_type = "text",
                                    title = dtAnkieta.Rows[0]["Odpowiedz5"].ToString(),
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Odpowiedz5_"+dtAnkieta.Rows[0]["ID"],
                                //       image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                },
                                                                                                                         new
                                {
                                    content_type = "text",
                                    title = dtAnkieta.Rows[0]["Odpowiedz6"].ToString(),
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Odpowiedz6_"+dtAnkieta.Rows[0]["ID"],
                                //       image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                },

                                     }
                        });

                        message.AttachmentLayout = null;

                        message.Text = dtAnkieta.Rows[0]["Tresc"].ToString();
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            try
                            {
                                if (fromId != dt.Rows[i]["UserId"].ToString())
                                {

                                    var userAccount = new ChannelAccount(name: dt.Rows[i]["UserName"].ToString(), id: dt.Rows[i]["UserId"].ToString());
                                    uzytkownik = userAccount.Name;
                                    var botAccount = new ChannelAccount(name: dt.Rows[i]["BotName"].ToString(), id: dt.Rows[i]["BotId"].ToString());
                                    var connector = new ConnectorClient(new Uri(dt.Rows[i]["Url"].ToString()), "39d46c3f-67d1-4cd1-b5b6-89ec124abf63", "6AnFsopfzqcb4reBLJpKMUB");
                                    var conversationId = await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount);
                                    message.From = botAccount;
                                    message.Recipient = userAccount;
                                    message.Conversation = new ConversationAccount(id: conversationId.Id, isGroup: false);
                                    //await connector.Conversations.SendToConversationAsync((Activity)message).ConfigureAwait(false);

                                    var returne = await connector.Conversations.SendToConversationAsync((Activity)message);
                                }
                            }
                            catch (Exception ex)
                            {
                                BaseDB.AddToLog("Błąd wysyłania wiadomości do: " + uzytkownik + " " + ex.ToString());
                            }
                        }
                    }


                    message.AttachmentLayout = null;


                    message.Text = dtAnkieta.Rows[0]["Tresc"].ToString();
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        try
                        {
                            if (fromId != dt.Rows[i]["UserId"].ToString())
                            {

                                var userAccount = new ChannelAccount(name: dt.Rows[i]["UserName"].ToString(), id: dt.Rows[i]["UserId"].ToString());
                                uzytkownik = userAccount.Name;
                                var botAccount = new ChannelAccount(name: dt.Rows[i]["BotName"].ToString(), id: dt.Rows[i]["BotId"].ToString());
                                var connector = new ConnectorClient(new Uri(dt.Rows[i]["Url"].ToString()), "39d46c3f-67d1-4cd1-b5b6-89ec124abf63", "6AnFsopfzqcb4reBLJpKMUB");
                                var conversationId = await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount);
                                message.From = botAccount;
                                message.Recipient = userAccount;
                                message.Conversation = new ConversationAccount(id: conversationId.Id, isGroup: false);
                                //await connector.Conversations.SendToConversationAsync((Activity)message).ConfigureAwait(false);

                                var returne = await connector.Conversations.SendToConversationAsync((Activity)message);
                            }
                        }
                        catch (Exception ex)
                        {
                            BaseDB.AddToLog("Błąd wysyłania wiadomości do: " + uzytkownik + " " + ex.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    BaseDB.AddToLog("Błąd wysyłania wiadomości do: " + uzytkownik + " " + ex.ToString());
                }
            }
            catch (Exception ex)
            {
                BaseDB.AddToLog("Błąd wysłania wiadomosci: " + ex.ToString());
            }
        }


        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                BaseDB.DeleteUser(message.From.Id);
            }
            else
                if (message.Type == ActivityTypes.ConversationUpdate)
            {
            }
            else
                    if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
            }
            else
                        if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else
                            if (message.Type == ActivityTypes.Ping)
            {
            }
            else
                                if (message.Type == ActivityTypes.Typing)
            {
            }
            return null;
        }
}

}
