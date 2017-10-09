using GksKatowiceBot.Helpers;
using HtmlAgilityPack;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace GksKatowiceBot.Controllers
{
    public class ThreadClass
    {
        private static void wb_DocumentCompleted(object sender, System.Windows.Forms.WebBrowserDocumentCompletedEventArgs e)
        {
            System.Windows.Forms.WebBrowser wb = sender as System.Windows.Forms.WebBrowser;
            // wb.Document is not null at this point
        }
        public static void SetShopInfo()
        {
            List<Attachment> list = new List<Attachment>();

            string urlAddress = "http://www.manufaktura.com/tenant/44/lista_sklepow";
            // string urlAddress = "http://www.orlenliga.pl/";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlAddress);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            var listTemp2 = new List<System.Linq.IGrouping<string, string>>();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                if (response.CharacterSet == null)
                {
                    readStream = new StreamReader(receiveStream);
                }
                else
                {
                    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                }

                string data = readStream.ReadToEnd();

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(data);

                string matchResultDivId = "subcontent";
                string xpath = String.Format("//div[@class='{0}']", matchResultDivId);
                var people = doc.DocumentNode.SelectNodes(xpath).Select(p => p.InnerHtml);
                string text = "";
                foreach (var person in people)
                {
                    text += person;
                }

                HtmlDocument doc2 = new HtmlDocument();

                doc2.LoadHtml(text);
                var hrefList = doc2.DocumentNode.SelectNodes("//tr")
                                  .Select(p => p.GetAttributeValue("onclick", "not found")).Where(p => p.Contains("scheme/")).GroupBy(p => p.ToString())
                                  .ToList();
                var titleList = doc2.DocumentNode.SelectNodes("//td[@class='desc']/span")
                                  
                                  .ToList();

                if(hrefList.Count>0)
                {
                    SqlConnection sqlConnection1 = new SqlConnection("Server=tcp:plps.database.windows.net,1433;Initial Catalog=PLPS;Persist Security Info=False;User ID=tomasoft;Password=Tomason18,;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
                    SqlCommand cmd = new SqlCommand();
                    SqlDataReader reader;


                    cmd.CommandText = "Delete from ManufakturaSklepy";
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = sqlConnection1;

                    sqlConnection1.Open();
                    cmd.ExecuteNonQuery();

                    sqlConnection1.Close();
                }


                for(int i=0; i<hrefList.Count;i++)
                {
                    try
                    {
                        SqlConnection sqlConnection1 = new SqlConnection("Server=tcp:plps.database.windows.net,1433;Initial Catalog=PLPS;Persist Security Info=False;User ID=tomasoft;Password=Tomason18,;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
                        SqlCommand cmd = new SqlCommand();
                        SqlDataReader reader;

                        string link = hrefList[i].Key.Replace("document.location.href='", "www.manufaktura.com").Replace("'", "");
                        string nazwa = titleList[i].InnerText.Replace("'", "''");
                        cmd.CommandText = @"Insert into ManufakturaSklepy (Tresc,Odpowiedz) values ('" + link + "','" + nazwa + "')";
                        cmd.CommandType = CommandType.Text;
                        cmd.Connection = sqlConnection1;

                        sqlConnection1.Open();
                        cmd.ExecuteNonQuery();

                        sqlConnection1.Close();
                    }
                    catch (Exception ex)
                    {

                    }
                }

                //foreach (var link in hrefList)
                //{

                //    urlAddress = link.Key.Replace("document.location.href='", "http://www.manufaktura.com");
                //    urlAddress = urlAddress.Replace("'", "");
                //    request = (HttpWebRequest)WebRequest.Create(urlAddress);
                //    response = (HttpWebResponse)request.GetResponse();

                //    listTemp2 = new List<System.Linq.IGrouping<string, string>>();

                //    if (response.StatusCode == HttpStatusCode.OK)
                //    {
                //        receiveStream = response.GetResponseStream();
                //        readStream = null;

                //        if (response.CharacterSet == null)
                //        {
                //            readStream = new StreamReader(receiveStream);
                //        }
                //        else
                //        {
                //            readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                //        }

                //        data = readStream.ReadToEnd();

                //        doc = new HtmlDocument();
                //        doc.LoadHtml(data);

                //        CustomBrowser browser = new CustomBrowser();
                //       browser.GetWebpage(urlAddress);


                //        matchResultDivId = "map_card";
                //        xpath = String.Format("//div[@id='{0}']/div", matchResultDivId);
                //        people = doc.DocumentNode.SelectNodes(xpath).Select(p => p.InnerHtml);
                //        text = "";
                //        foreach (var person in people)
                //        {
                //            text += person;
                //        }

                //        doc2 = new HtmlDocument();

                //        doc2.LoadHtml(text);

                //        var imgList = doc2.DocumentNode.SelectNodes("//img")
                //                          .Select(p => p.GetAttributeValue("src", "not found"))
                //                          .ToList();

                //        //var titleList = doc2.DocumentNode.SelectNodes("//div[@class='news-info']")
                //        //                  .Select(p => p.GetAttributeValue("alt", "not found"))
                //        //                  .ToList();

                //        var titleList = doc2.DocumentNode.SelectNodes("//div[@class='phone']").Where(p => p.InnerText != "więcej...").Where(p => p.InnerText != "  ")
                //                          .ToList();
                //        var titleList2 = doc2.DocumentNode.SelectNodes("//div[@class='email']").Where(p => p.InnerText != "więcej...").Where(p => p.InnerText != "  ")
                //      .ToList();

                //    }
                //}





                //public async static void SendThreadMessage()
                //{
                //    try
                //    {
                //        if (DateTime.UtcNow.Hour == 9 && (DateTime.UtcNow.Minute > 0 && DateTime.UtcNow.Minute <= 3))
                //        {
                //            BaseDB.AddToLog("Wywołanie metody SendThreadMessage");

                //            List<IGrouping<string, string>> hrefList = new List<IGrouping<string, string>>();
                //            List<IGrouping<string, string>> hrefList2 = new List<IGrouping<string, string>>();
                //            List<IGrouping<string, string>> hreflist3 = new List<IGrouping<string, string>>();
                //            List<IGrouping<string, string>> hreflist4 = new List<IGrouping<string, string>>();
                //            var items = BaseGETMethod.GetCardsAttachments(ref hrefList);
                //            hreflist3 = hrefList;
                //            var items2 = BaseGETMethod.GetCardsAttachmentsOrlenLiga(ref hrefList2);
                //            var items4 = BaseGETMethod.GetCardsAttachmentsHokej(ref hreflist4);

                //            var items3 = new List<Attachment>();

                //            if(items.Count>0)
                //            {
                //                items3.Add(items[0]);
                //            }
                //            if(items2.Count>0)
                //            {
                //                items3.Add(items2[0]);
                //            }
                //            if(items4.Count>0)
                //            {
                //                items3.Add(items4[0]);
                //            }

                //            items = items3;


                //            string uzytkownik = "";
                //            DataTable dt = BaseGETMethod.GetUser();

                //            if (items.Count > 0)
                //            {
                //                try
                //                {
                //                    MicrosoftAppCredentials.TrustServiceUrl(@"https://facebook.botframework.com", DateTime.MaxValue);

                //                    IMessageActivity message = Activity.CreateMessageActivity();
                //                    message.ChannelData = JObject.FromObject(new
                //                    {
                //                        notification_type = "REGULAR",
                //                        quick_replies = new dynamic[]
                //                            {
                //                     new
                //                        {
                //                            content_type = "text",
                //                            title = "Piłka nożna",
                //                            payload = "DEVELOPER_DEFINED_PAYLOAD_Pilka_Nozna",
                //                            //     image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                //                    //        image_url = "http://archiwum.koluszki.pl/zdjecia/naglowki_nowe/listopad%202013/pi%C5%82ka[1].png"
                //                        },
                //                        new
                //                        {
                //                            content_type = "text",
                //                            title = "Siatkówka",
                //                            payload = "DEVELOPER_DEFINED_PAYLOAD_Siatkowka",
                //               //             image_url = "https://gim7bytom.edupage.org/global/pics/iconspro/sport/volleyball.png"
                //                        },                                new
                //                        {
                //                            content_type = "text",
                //                            title = "Hokej",
                //                            payload = "DEVELOPER_DEFINED_PAYLOAD_Hokej",
                //                        //       image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                //                        },
                //                                                           }
                //                    });

                //                    message.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                //                    message.Attachments = items;
                //                    for (int i = 0; i < dt.Rows.Count; i++)
                //                    {
                //                        try
                //                        {
                //                            var userAccount = new ChannelAccount(name: dt.Rows[i]["UserName"].ToString(), id: dt.Rows[i]["UserId"].ToString());
                //                            uzytkownik = userAccount.Name;
                //                            var botAccount = new ChannelAccount(name: dt.Rows[i]["BotName"].ToString(), id: dt.Rows[i]["BotId"].ToString());
                //                            var connector = new ConnectorClient(new Uri(dt.Rows[i]["Url"].ToString()), "73267226-823f-46b0-8303-2e866165a3b2", "k6XBDCgzL5452YDhS3PPLsL");
                //                            var conversationId = await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount);
                //                            message.From = botAccount;
                //                            message.Recipient = userAccount;
                //                            message.Conversation = new ConversationAccount(id: conversationId.Id, isGroup: false);
                //                            await connector.Conversations.SendToConversationAsync((Activity)message).ConfigureAwait(false);
                //                        }
                //                        catch (Exception ex)
                //                        {
                //                            BaseDB.AddToLog("Błąd wysyłania wiadomości do: " + uzytkownik + " " + ex.ToString());
                //                        }
                //                    }
                //                }
                //                catch (Exception ex)
                //                {
                //                    BaseDB.AddToLog("Błąd wysyłania wiadomości do: " + uzytkownik + " " + ex.ToString());
                //                }


                //                BaseDB.AddWiadomoscPilka(hreflist3);
                //                BaseDB.AddWiadomoscSiatka(hrefList2);
                //                BaseDB.AddWiadomoscHokej(hreflist4);
                //            }
                //        }
                //    }
                //    catch (Exception ex)
                //    {
                //        BaseDB.AddToLog("Błąd wysłania wiadomosci: " + ex.ToString());
                //    }
                //}

           
            }
        }

        public async static void WyslijOdpowiedz()
        {
            try
            {
                DataTable answerWait = BaseDB.wyslijOdpowiedz();

                foreach (var item in answerWait.AsEnumerable())
                {
                    string uzytkownik = "";
                    DataTable dt = BaseGETMethod.GetUserById(Convert.ToInt64(item["WiadomoscOdId"]));

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
                                    title = "Wydarzenia",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Aktualnosci",
                                    //     image_url = "https://cdn3.iconfinder.com/data/icons/developperss/PNG/Green%20Ball.png"
                                 //   image_url = "http://archiwum.koluszki.pl/zdjecia/naglowki_nowe/listopad%202013/pi%C5%82ka[1].png"
                                },
                                new
                                {
                                    content_type = "text",
                                    title = "Zakupy",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Zakupy",
                               //       image_url = "https://gim7bytom.edupage.org/global/pics/iconspro/sport/volleyball.png"
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
                                new
                                {
                                    content_type = "text",
                                    title = "Restauracje",
                                    payload = "DEVELOPER_DEFINED_PAYLOAD_Jedzenie",
                               //       image_url = "https://gim7bytom.edupage.org/global/pics/iconspro/sport/volleyball.png"
                                },
                                                                      }
                        });


                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            try
                            {
                                message.Text = item["TrescOdpowiedzi"].ToString();
                                var userAccount = new ChannelAccount(name: dt.Rows[i]["UserName"].ToString(), id: dt.Rows[i]["UserId"].ToString());
                                uzytkownik = userAccount.Name;
                                var botAccount = new ChannelAccount(name: dt.Rows[i]["BotName"].ToString(), id: dt.Rows[i]["BotId"].ToString());
                                var connector = new ConnectorClient(new Uri(dt.Rows[i]["Url"].ToString()), "39d46c3f-67d1-4cd1-b5b6-89ec124abf63", "6AnFsopfzqcb4reBLJpKMUB");
                                var conversationId = await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount);
                                message.From = botAccount;
                                message.Recipient = userAccount;
                                message.Conversation = new ConversationAccount(id: conversationId.Id, isGroup: false);
                                await connector.Conversations.SendToConversationAsync((Activity)message).ConfigureAwait(false);

                                BaseDB.UsunWiadomosc(item["Tresc"].ToString(), Convert.ToInt64(item["WiadomoscOdId"]));
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
            }
            catch (Exception ex)
            {
                BaseDB.AddToLog("Błąd wysłania wiadomosci: " + ex.ToString());
            }
        }
    }
}