using HtmlAgilityPack;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace GksKatowiceBot.Helpers
{
    public class BaseGETMethod
    {
        public static IList<Attachment> GetCardsAttachmentsAktualnosci(ref List<IGrouping<string, string>> hrefList, bool newUser = false)
        {
            List<Attachment> list = new List<Attachment>();

            string urlAddress = "http://www.manufaktura.com";
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

                string matchResultDivId = "event-desc";
                string xpath = String.Format("//div[@class='{0}']", matchResultDivId);
                var people = doc.DocumentNode.SelectNodes(xpath).Select(p => p.InnerHtml);
                string text = "";
                foreach (var person in people)
                {
                    text += person;
                }

                HtmlDocument doc2 = new HtmlDocument();

                doc2.LoadHtml(text);
                hrefList = doc2.DocumentNode.SelectNodes("//div")
                                  .Select(p => p.GetAttributeValue("onclick", "not found")).Where(p => p.Contains("events/")).GroupBy(p => p.ToString())
                                  .ToList();

                var imgList = doc2.DocumentNode.SelectNodes("//img")
                                  .Select(p => p.GetAttributeValue("data-file", "not found"))
                                  .ToList();

                //var titleList = doc2.DocumentNode.SelectNodes("//div[@class='news-info']")
                //                  .Select(p => p.GetAttributeValue("alt", "not found"))
                //                  .ToList();

                var titleList = doc2.DocumentNode.SelectNodes("//span").Where(p => p.InnerHtml != "")
                                  .ToList();

                response.Close();
                readStream.Close();

                int index = 5;

                DataTable dt = GetWiadomosci();
                if (newUser == true)
                {
                    index = hrefList.Count;
                    if (dt.Rows.Count == 0)
                    {
                        //    AddWiadomosc(hrefList);
                    }
                }

                else
                {
                    if (dt.Rows.Count > 0)
                    {
                        List<int> deleteList = new List<int>();
                        var listTemp = new List<System.Linq.IGrouping<string, string>>();
                        var imageListTemp = new List<string>();
                        var titleListTemp = new List<string>();

                        int ilosc = hrefList.Count;

                        for (int i = 0; i < ilosc; i++)
                        {
                            if (dt.Rows[dt.Rows.Count - 1]["Wiadomosc1"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc2"].ToString() != hrefList[i].Key &&
                                dt.Rows[dt.Rows.Count - 1]["Wiadomosc3"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc4"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc5"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc6"].ToString() != hrefList[i].Key
                                && dt.Rows[dt.Rows.Count - 1]["Wiadomosc7"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc8"].ToString() != hrefList[i].Key
                                && dt.Rows[dt.Rows.Count - 1]["Wiadomosc9"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc10"].ToString() != hrefList[i].Key
                                )
                            {
                                listTemp.Add(hrefList[i]);
                                imageListTemp.Add("http://galeriaecho.pl" + imgList[i]);
                                //        titleListTemp.Add(titleList[i].Replace("&quot;", ""));
                            }
                            listTemp2.Add(hrefList[i]);
                        }
                        hrefList = listTemp;
                        index = hrefList.Count;
                        imgList = imageListTemp;
                        //    titleList = titleListTemp;
                        //   AddWiadomosc(listTemp2);
                    }
                    else
                    {
                        index = 5;
                        //   AddWiadomosc(hrefList);
                    }
                }

                for (int i = 0; i < index; i++)
                {
                    string link = "http://www.manufaktura.com / " + hrefList[i].Key.ToString().Substring(hrefList[i].Key.ToString().IndexOf("events"));
                    link = link.Replace(" ", "").Replace("'", "");
                    string image = imgList[i].Replace("event", "event/&file=");
                    list.Add(GetHeroCard(
                    titleList[i].InnerHtml.ToString().Replace("&quot;", ""), "", "",
                    new CardImage(url: "http://www.manufaktura.com/thumb.php?dir=files/" + image.Remove(image.IndexOf("jpg") + 3) + "&w=712&h=315&isframe=1&frametype=center&horizontal=center"),
                    new CardAction(ActionTypes.OpenUrl, "Więcej", value: link),
                    new CardAction(ActionTypes.OpenUrl, "Udostępnij", value: "https://www.facebook.com/sharer/sharer.php?u=" + link))
                    );

                }
            }
            if (listTemp2.Count > 0)
            {
                hrefList = listTemp2;
            }

            return list;
        }


        public static IList<Attachment> GetCardsAttachmentsWyszukiwarka(ref List<IGrouping<string, string>> hrefList, bool newUser = false, string haslo = "")
        {
            List<Attachment> list = new List<Attachment>();
            if (haslo != "")
            {
                string urlAddress = "http://www.manufaktura.com/search/" + haslo;
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

                    string matchResultDivId = "search_results";
                    string xpath = String.Format("//div[@class='{0}']", matchResultDivId);
                    var people = doc.DocumentNode.SelectNodes(xpath).Select(p => p.InnerHtml);
                    string text = "";
                    foreach (var person in people)
                    {
                        text += person;
                    }

                    HtmlDocument doc2 = new HtmlDocument();

                    doc2.LoadHtml(text);
                    if (text != " ")
                    {
                        hrefList = doc2.DocumentNode.SelectNodes("//a")
                                          .Select(p => p.GetAttributeValue("href", "not found")).GroupBy(p => p.ToString())
                                          .ToList();

                        // var imgList = doc2.DocumentNode.SelectNodes("//img")
                        //                   .Select(p => p.GetAttributeValue("data-file", "not found"))
                        //                   .ToList();

                        //var titleList = doc2.DocumentNode.SelectNodes("//div[@class='news-info']")
                        //                  .Select(p => p.GetAttributeValue("alt", "not found"))
                        //                  .ToList();

                        var titleList = doc2.DocumentNode.SelectNodes("//h2").Where(p => p.InnerHtml != "")
                                          .ToList();

                        response.Close();
                        readStream.Close();

                        int index = 5;

                        DataTable dt = new DataTable();
                        if (newUser == true)
                        {
                            index = hrefList.Count;
                            if (dt.Rows.Count == 0)
                            {
                                //    AddWiadomosc(hrefList);
                            }
                        }

                        else
                        {
                            if (dt.Rows.Count > 0)
                            {
                                List<int> deleteList = new List<int>();
                                var listTemp = new List<System.Linq.IGrouping<string, string>>();
                                var imageListTemp = new List<string>();
                                var titleListTemp = new List<string>();

                                int ilosc = hrefList.Count;

                                for (int i = 0; i < ilosc; i++)
                                {
                                    if (dt.Rows[dt.Rows.Count - 1]["Wiadomosc1"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc2"].ToString() != hrefList[i].Key &&
                                        dt.Rows[dt.Rows.Count - 1]["Wiadomosc3"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc4"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc5"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc6"].ToString() != hrefList[i].Key
                                        && dt.Rows[dt.Rows.Count - 1]["Wiadomosc7"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc8"].ToString() != hrefList[i].Key
                                        && dt.Rows[dt.Rows.Count - 1]["Wiadomosc9"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc10"].ToString() != hrefList[i].Key
                                        )
                                    {
                                        listTemp.Add(hrefList[i]);
                                        // imageListTemp.Add("http://galeriaecho.pl" + imgList[i]);
                                        //        titleListTemp.Add(titleList[i].Replace("&quot;", ""));
                                    }
                                    listTemp2.Add(hrefList[i]);
                                }
                                hrefList = listTemp;
                                index = hrefList.Count;
                                //    imgList = imageListTemp;
                                //    titleList = titleListTemp;
                                //   AddWiadomosc(listTemp2);
                            }
                            else
                            {
                                index = hrefList.Count;
                                //   AddWiadomosc(hrefList);
                            }
                        }

                        for (int i = 0; i < index; i++)
                        {
                            string link = "http://www.manufaktura.com / " + hrefList[i].Key.ToString();
                            link = link.Replace(" ", "").Replace("'", "");
                            string image = "";
                            list.Add(GetHeroCard(
                            titleList[i].InnerText.ToString().Replace("&quot;", ""), "", "",
                            new CardImage(url: ""),
                            new CardAction(ActionTypes.OpenUrl, "Więcej", value: link),
                            new CardAction(ActionTypes.OpenUrl, "Udostępnij", value: "https://www.facebook.com/sharer/sharer.php?u=" + link))
                            );

                        }
                    }
                    if (listTemp2.Count > 0)
                    {
                        hrefList = listTemp2;
                    }
                }
            }
            return list;
        }


        public static IList<Attachment> GetCardsAttachmentsSklepy(ref List<IGrouping<string, string>> hrefList, bool newUser = false, string nazwaSklepu = "")
        {
            List<Attachment> list = new List<Attachment>();
            if (nazwaSklepu != "")
            {
                int index = 0;
                var listaSlow = nazwaSklepu.Split(' ');
                for (int j = 0; j < listaSlow.Count(); j++)
                {
                    if (index == 0 && listaSlow[j].Length>2)
                    {
                        
                        SqlConnection sqlConnection1 = new SqlConnection("Server=tcp:plps.database.windows.net,1433;Initial Catalog=PLPS;Persist Security Info=False;User ID=tomasoft;Password=Tomason18,;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
                        SqlCommand cmd = new SqlCommand();
                        SqlDataReader reader;

                        cmd.CommandText = @"Select * from ManufakturaSklepy where Odpowiedz like'%" + listaSlow[j].Replace("?", "").Replace(".", "").Replace("-", "").Replace("!", "").Replace(",", "") + "'";
                        cmd.CommandType = CommandType.Text;
                        cmd.Connection = sqlConnection1;


                        DataTable dt = new DataTable();
                        sqlConnection1.Open();
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        // this will query your database and return the result to your datatable
                        da.Fill(dt);
                        sqlConnection1.Close();
                        da.Dispose();

                        if (j + 1 < listaSlow.Count())
                        {
                            try
                            {
                                dt = dt.Select("Odpowiedz like '%" + listaSlow[j + 1] + "'").CopyToDataTable();
                            }
                            catch
                            {

                            }
                        }
                        index = dt.Rows.Count;
                        for (int i = 0; i < index; i++)
                        {
                            string link = "http://" + dt.Rows[i]["Tresc"].ToString();
                            link = link.Replace(" ", "").Replace("'", "");
                            string image = "";
                            list.Add(GetHeroCard(
                            dt.Rows[i]["Odpowiedz"].ToString(), "", "",
                            new CardImage(url: ""),
                            new CardAction(ActionTypes.OpenUrl, "Więcej", value: link),
                            new CardAction(ActionTypes.OpenUrl, "Udostępnij", value: "https://www.facebook.com/sharer/sharer.php?u=" + link))
                            );

                        }
                    }
                }
            }

            return list;
        }


        public static DataTable GetCardsAttachmentsSprawdzSzablon(ref List<IGrouping<string, string>> hrefList, bool newUser = false, string Tekst = "")
        {
            DataTable dt = BaseDB.sprawdzSzablon(Tekst);
            return dt;
        }

        public static IList<Attachment> GetCardsAttachmentsPromocje(ref List<IGrouping<string, string>> hrefList, bool newUser = false)
        {
            List<Attachment> list = new List<Attachment>();

            string urlAddress = "http://www.manufaktura.com/news/4/nie_przegap";
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

                string matchResultDivId = "subcontent full";
                string xpath = String.Format("//div[@class='{0}']", matchResultDivId);
                var people = doc.DocumentNode.SelectNodes(xpath).Select(p => p.InnerHtml);
                string text = "";
                foreach (var person in people)
                {
                    text += person;
                }

                HtmlDocument doc2 = new HtmlDocument();

                doc2.LoadHtml(text);
                hrefList = doc2.DocumentNode.SelectNodes("//a")
                                  .Select(p => p.GetAttributeValue("href", "not found")).GroupBy(p => p.ToString())
                                  .ToList();

                var imgList = doc2.DocumentNode.SelectNodes("//img")
                                  .Select(p => p.GetAttributeValue("src", "not found"))
                                  .ToList();

                //var titleList = doc2.DocumentNode.SelectNodes("//div[@class='news-info']")
                //                  .Select(p => p.GetAttributeValue("alt", "not found"))
                //                  .ToList();

                var titleList = doc2.DocumentNode.SelectNodes("//a").Where(p => p.InnerText != "więcej...").Where(p => p.InnerText != "  ")
                                  .ToList();

                response.Close();
                readStream.Close();

                int index = 5;

                DataTable dt = GetWiadomosci();
                if (newUser == true)
                {
                    index = hrefList.Count;
                    if (dt.Rows.Count == 0)
                    {
                        //    AddWiadomosc(hrefList);
                    }
                }

                else
                {
                    if (dt.Rows.Count > 0)
                    {
                        List<int> deleteList = new List<int>();
                        var listTemp = new List<System.Linq.IGrouping<string, string>>();
                        var imageListTemp = new List<string>();
                        var titleListTemp = new List<string>();

                        int ilosc = hrefList.Count;

                        for (int i = 0; i < ilosc; i++)
                        {
                            if (dt.Rows[dt.Rows.Count - 1]["Wiadomosc1"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc2"].ToString() != hrefList[i].Key &&
                                dt.Rows[dt.Rows.Count - 1]["Wiadomosc3"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc4"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc5"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc6"].ToString() != hrefList[i].Key
                                && dt.Rows[dt.Rows.Count - 1]["Wiadomosc7"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc8"].ToString() != hrefList[i].Key
                                && dt.Rows[dt.Rows.Count - 1]["Wiadomosc9"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc10"].ToString() != hrefList[i].Key
                                )
                            {
                                listTemp.Add(hrefList[i]);
                                imageListTemp.Add("http://galeriaecho.pl" + imgList[i]);
                                //        titleListTemp.Add(titleList[i].Replace("&quot;", ""));
                            }
                            listTemp2.Add(hrefList[i]);
                        }
                        hrefList = listTemp;
                        index = hrefList.Count;
                        imgList = imageListTemp;
                        //    titleList = titleListTemp;
                        //   AddWiadomosc(listTemp2);
                    }
                    else
                    {
                        index = 5;
                        //   AddWiadomosc(hrefList);
                    }
                }

                if (index > 10)
                    index = 10;

                for (int i = 0; i < index; i++)
                {
                    string link = "http://www.manufaktura.com / " + hrefList[i].Key.ToString().Replace(" ", "");
                    link = link.Replace(" ", "").Replace("'", "");
                    string image = imgList[i];
                    image = "http://www.manufaktura.com/" + image.Remove(image.IndexOf("jpg") + 3);
                    image = image.Replace("amp;", "") + "&w=300";
                    list.Add(GetHeroCard(
                    titleList[i].InnerText.ToString().Replace("&quot;", ""), "", "",
                    new CardImage(url: image),
                    new CardAction(ActionTypes.OpenUrl, "Więcej", value: link),
                    new CardAction(ActionTypes.OpenUrl, "Udostępnij", value: "https://www.facebook.com/sharer/sharer.php?u=" + link))
                    );

                }
            }
            if (listTemp2.Count > 0)
            {
                hrefList = listTemp2;
            }

            return list;
        }

        public static IList<Attachment> GetCardsAttachmentsKulturaSztuka(ref List<IGrouping<string, string>> hrefList, bool newUser = false)
        {
            List<Attachment> list = new List<Attachment>();

            string urlAddress = "http://www.manufaktura.com/5/kultura_i_sztuka";
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

                string matchResultDivId = "gallery";
                string xpath = String.Format("//div[@class='{0}']", matchResultDivId);
                var people = doc.DocumentNode.SelectNodes(xpath).Select(p => p.InnerHtml);
                string text = "";
                foreach (var person in people)
                {
                    text += person;
                }

                HtmlDocument doc2 = new HtmlDocument();

                doc2.LoadHtml(text);
                hrefList = doc2.DocumentNode.SelectNodes("//a")
                                  .Select(p => p.GetAttributeValue("href", "not found")).GroupBy(p => p.ToString())
                                  .ToList();

                var imgList = doc2.DocumentNode.SelectNodes("//img")
                                  .Select(p => p.GetAttributeValue("src", "not found"))
                                  .ToList();

                //var titleList = doc2.DocumentNode.SelectNodes("//div[@class='news-info']")
                //                  .Select(p => p.GetAttributeValue("alt", "not found"))
                //                  .ToList();

                var titleList = doc2.DocumentNode.SelectNodes("//div[@class='img_title']").Where(p => p.InnerText != "więcej...").Where(p => p.InnerText != "  ")
                                  .ToList();
                var titleList2 = titleList.GroupBy(p => p.InnerText).Select(x => x.Key);

                response.Close();
                readStream.Close();

                int index = 5;

                DataTable dt = GetWiadomosci();
                if (newUser == true)
                {
                    index = hrefList.Count;
                    if (dt.Rows.Count == 0)
                    {
                        //    AddWiadomosc(hrefList);
                    }
                }

                else
                {
                    if (dt.Rows.Count > 0)
                    {
                        List<int> deleteList = new List<int>();
                        var listTemp = new List<System.Linq.IGrouping<string, string>>();
                        var imageListTemp = new List<string>();
                        var titleListTemp = new List<string>();

                        int ilosc = hrefList.Count;

                        for (int i = 0; i < ilosc; i++)
                        {
                            if (dt.Rows[dt.Rows.Count - 1]["Wiadomosc1"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc2"].ToString() != hrefList[i].Key &&
                                dt.Rows[dt.Rows.Count - 1]["Wiadomosc3"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc4"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc5"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc6"].ToString() != hrefList[i].Key
                                && dt.Rows[dt.Rows.Count - 1]["Wiadomosc7"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc8"].ToString() != hrefList[i].Key
                                && dt.Rows[dt.Rows.Count - 1]["Wiadomosc9"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc10"].ToString() != hrefList[i].Key
                                )
                            {
                                listTemp.Add(hrefList[i]);
                                imageListTemp.Add("http://galeriaecho.pl" + imgList[i]);
                                //        titleListTemp.Add(titleList[i].Replace("&quot;", ""));
                            }
                            listTemp2.Add(hrefList[i]);
                        }
                        hrefList = listTemp;
                        index = hrefList.Count;
                        imgList = imageListTemp;
                        //    titleList = titleListTemp;
                        //   AddWiadomosc(listTemp2);
                    }
                    else
                    {
                        index = 5;
                        //   AddWiadomosc(hrefList);
                    }
                }

                if (index > 10)
                    index = 10;

                for (int i = 0; i < index; i++)
                {
                    string link = "http://www.manufaktura.com / " + hrefList[i].Key.ToString().Replace(" ", "");
                    link = link.Replace(" ", "").Replace("'", "");
                    string image = imgList[i];
                    image = "http://www.manufaktura.com/" + image.Remove(image.IndexOf("jpg") + 3);
                    image = image.Replace("amp;", "") + "&w=300";
                    list.Add(GetHeroCard(
                    titleList2.ToList()[i].ToString().Replace("&quot;", ""), "", "",
                    new CardImage(url: image),
                    new CardAction(ActionTypes.OpenUrl, "Więcej", value: link),
                    new CardAction(ActionTypes.OpenUrl, "Udostępnij", value: "https://www.facebook.com/sharer/sharer.php?u=" + link))
                    );

                }
            }
            if (listTemp2.Count > 0)
            {
                hrefList = listTemp2;
            }

            return list;
        }

        public static IList<Attachment> GetCardsAttachmentsRozrywka(ref List<IGrouping<string, string>> hrefList, bool newUser = false)
        {
            List<Attachment> list = new List<Attachment>();

            string urlAddress = "http://www.manufaktura.com/6/rozrywka";
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

                string matchResultDivId = "gallery";
                string xpath = String.Format("//div[@class='{0}']", matchResultDivId);
                var people = doc.DocumentNode.SelectNodes(xpath).Select(p => p.InnerHtml);
                string text = "";
                foreach (var person in people)
                {
                    text += person;
                }

                HtmlDocument doc2 = new HtmlDocument();

                doc2.LoadHtml(text);
                hrefList = doc2.DocumentNode.SelectNodes("//a")
                                  .Select(p => p.GetAttributeValue("href", "not found")).GroupBy(p => p.ToString())
                                  .ToList();

                var imgList = doc2.DocumentNode.SelectNodes("//img")
                                  .Select(p => p.GetAttributeValue("src", "not found"))
                                  .ToList();

                //var titleList = doc2.DocumentNode.SelectNodes("//div[@class='news-info']")
                //                  .Select(p => p.GetAttributeValue("alt", "not found"))
                //                  .ToList();

                var titleList = doc2.DocumentNode.SelectNodes("//div[@class='img_title']").Where(p => p.InnerText != "więcej...").Where(p => p.InnerText != "  ")
                                  .ToList();

                response.Close();
                readStream.Close();

                int index = 5;

                DataTable dt = GetWiadomosci();
                if (newUser == true)
                {
                    index = hrefList.Count;
                    if (dt.Rows.Count == 0)
                    {
                        //    AddWiadomosc(hrefList);
                    }
                }

                else
                {
                    if (dt.Rows.Count > 0)
                    {
                        List<int> deleteList = new List<int>();
                        var listTemp = new List<System.Linq.IGrouping<string, string>>();
                        var imageListTemp = new List<string>();
                        var titleListTemp = new List<string>();

                        int ilosc = hrefList.Count;

                        for (int i = 0; i < ilosc; i++)
                        {
                            if (dt.Rows[dt.Rows.Count - 1]["Wiadomosc1"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc2"].ToString() != hrefList[i].Key &&
                                dt.Rows[dt.Rows.Count - 1]["Wiadomosc3"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc4"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc5"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc6"].ToString() != hrefList[i].Key
                                && dt.Rows[dt.Rows.Count - 1]["Wiadomosc7"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc8"].ToString() != hrefList[i].Key
                                && dt.Rows[dt.Rows.Count - 1]["Wiadomosc9"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc10"].ToString() != hrefList[i].Key
                                )
                            {
                                listTemp.Add(hrefList[i]);
                                imageListTemp.Add("http://galeriaecho.pl" + imgList[i]);
                                //        titleListTemp.Add(titleList[i].Replace("&quot;", ""));
                            }
                            listTemp2.Add(hrefList[i]);
                        }
                        hrefList = listTemp;
                        index = hrefList.Count;
                        imgList = imageListTemp;
                        //    titleList = titleListTemp;
                        //   AddWiadomosc(listTemp2);
                    }
                    else
                    {
                        index = 5;
                        //   AddWiadomosc(hrefList);
                    }
                }

                if (index > 10)
                    index = 10;

                for (int i = 0; i < index; i++)
                {
                    string link = "http://www.manufaktura.com / " + hrefList[i].Key.ToString().Replace(" ", "");
                    link = link.Replace(" ", "").Replace("'", "");
                    string image = imgList[i];
                    image = "http://www.manufaktura.com/" + image.Remove(image.IndexOf("jpg") + 3);
                    image = image.Replace("amp;", "") + "&w=300";
                    list.Add(GetHeroCard(
                    titleList[i].InnerText.ToString().Replace("&quot;", ""), "", "",
                    new CardImage(url: image),
                    new CardAction(ActionTypes.OpenUrl, "Więcej", value: link),
                    new CardAction(ActionTypes.OpenUrl, "Udostępnij", value: "https://www.facebook.com/sharer/sharer.php?u=" + link))
                    );

                }
            }
            if (listTemp2.Count > 0)
            {
                hrefList = listTemp2;
            }

            return list;
        }

        //public static IList<Attachment> GetCardsAttachments(ref List<IGrouping<string, string>> hrefList, bool newUser = false)
        //{
        ////    List<Attachment> list = new List<Attachment>();

        ////    string urlAddress = "http://www.gkskatowice.eu/index";
        ////    // string urlAddress = "http://www.orlenliga.pl/";

        ////    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlAddress);
        ////    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

        ////    var listTemp2 = new List<System.Linq.IGrouping<string, string>>();

        ////    if (response.StatusCode == HttpStatusCode.OK)
        ////    {
        ////        Stream receiveStream = response.GetResponseStream();
        ////        StreamReader readStream = null;

        ////        if (response.CharacterSet == null)
        ////        {
        ////            readStream = new StreamReader(receiveStream);
        ////        }
        ////        else
        ////        {
        ////            readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
        ////        }

        ////        string data = readStream.ReadToEnd();

        ////        HtmlDocument doc = new HtmlDocument();
        ////        doc.LoadHtml(data);

        ////        string matchResultDivId = "carousel-inner";
        ////        string xpath = String.Format("//div[@class='{0}']/div", matchResultDivId);
        ////        var people = doc.DocumentNode.SelectNodes(xpath).Select(p => p.InnerHtml);
        ////        string text = "";
        ////        foreach (var person in people)
        ////        {
        ////            text += person;
        ////        }

        ////        HtmlDocument doc2 = new HtmlDocument();

        ////        doc2.LoadHtml(text);
        ////        hrefList = doc2.DocumentNode.SelectNodes("//a")
        ////                          .Select(p => p.GetAttributeValue("href", "not found")).Where(p => p.Contains("/n/") || p.Contains("/video/") || p.Contains("/gallery/") || p.Contains("/blog/")).GroupBy(p => p.ToString())
        ////                          .ToList();

        ////        var imgList = doc2.DocumentNode.SelectNodes("//img")
        ////                          .Select(p => p.GetAttributeValue("src", "not found"))
        ////                          .ToList();

        ////        var titleList = doc2.DocumentNode.SelectNodes("//img")
        ////                          .Select(p => p.GetAttributeValue("alt", "not found"))
        ////                          .ToList();

        ////        response.Close();
        ////        readStream.Close();

        ////        int index = 5;

        ////        DataTable dt = GetWiadomosciPilka();

        ////        if (newUser == true)
        ////        {
        ////            index = 5;
        ////            if (dt.Rows.Count == 0)
        ////            {
        ////                //    AddWiadomosc(hrefList);
        ////            }
        ////        }

        ////        else
        ////        {
        ////            if (dt.Rows.Count > 0)
        ////            {
        ////                List<int> deleteList = new List<int>();
        ////                var listTemp = new List<System.Linq.IGrouping<string, string>>();
        ////                var imageListTemp = new List<string>();
        ////                var titleListTemp = new List<string>();

        ////                for (int i = 0; i < hrefList.Count; i++)
        ////                {
        ////                    if (dt.Rows[dt.Rows.Count - 1]["Wiadomosc1"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc2"].ToString() != hrefList[i].Key &&
        ////                        dt.Rows[dt.Rows.Count - 1]["Wiadomosc3"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc4"].ToString() != hrefList[i].Key && dt.Rows[dt.Rows.Count - 1]["Wiadomosc5"].ToString() != hrefList[i].Key
        ////                    )
        ////                    {
        ////                        listTemp.Add(hrefList[i]);
        ////                        imageListTemp.Add("http://www.gkskatowice.eu" + imgList[i]);
        ////                        titleListTemp.Add(titleList[i].Replace("&quot;", ""));
        ////                    }
        ////                    listTemp2.Add(hrefList[i]);
        ////                }
        ////                hrefList = listTemp;
        ////                index = hrefList.Count;
        ////                imgList = imageListTemp;
        ////                titleList = titleListTemp;
        ////                //   AddWiadomosc(listTemp2);
        ////            }
        ////            else
        ////            {
        ////                index = 5;
        ////                //   AddWiadomosc(hrefList);
        ////            }
        ////        }

        ////        for (int i = 0; i < index; i++)
        ////        {
        ////            string link = "";
        ////            if (hrefList[i].Key.Contains("http"))
        ////            {
        ////                link = hrefList[i].Key;
        ////            }
        ////            else
        ////            {
        ////                link = "http://www.gkskatowice.eu" + hrefList[i].Key;
        ////                //link = "http://www.orlenliga.pl/" + hrefList[i].Key;
        ////            }

        ////            if (link.Contains("video"))
        ////            {
        ////                list.Add(GetHeroCard(
        ////                titleList[i].Replace("&quot;", ""), "", "",
        ////                new CardImage(url: imgList[i]),
        ////                new CardAction(ActionTypes.OpenUrl, "Oglądaj video", value: link),
        ////                new CardAction(ActionTypes.OpenUrl, "Udostępnij", value: "https://www.facebook.com/sharer/sharer.php?u=" + link))
        ////                );
        ////            }
        ////            else
        ////                if (link.Contains("gallery"))
        ////            {
        ////                list.Add(GetHeroCard(
        ////                titleList[i].Replace("&quot;", ""), "", "",
        ////                new CardImage(url: imgList[i]),
        ////                new CardAction(ActionTypes.OpenUrl, "Przeglądaj galerie", value: link),
        ////                new CardAction(ActionTypes.OpenUrl, "Udostępnij", value: "https://www.facebook.com/sharer/sharer.php?u=" + link))
        ////                );
        ////            }
        ////            else
        ////            {
        ////                list.Add(GetHeroCard(
        ////                titleList[i].Replace("&quot;", ""), "", "",
        ////                new CardImage(url: "http://www.gkskatowice.eu" + imgList[i]),
        ////                new CardAction(ActionTypes.OpenUrl, "Więcej", value: link),
        ////                new CardAction(ActionTypes.OpenUrl, "Udostępnij", value: "https://www.facebook.com/sharer/sharer.php?u=" + link))
        ////                );
        ////            }

        ////            //  list.Add(new Microsoft.Bot.Connector.VideoCard(titleList[i], "", "",null)
        ////        }
        ////    }
        ////    if (listTemp2.Count > 0)
        ////    {
        ////        hrefList = listTemp2;
        ////    }

        ////    return list;

        //}




        private static Attachment GetHeroCard(string title, string subtitle, string text, CardImage cardImage, CardAction cardAction, CardAction cardAction2)
        {
            if (cardAction2 != null)
            {
                var heroCard = new HeroCard
                {
                    Title = title,
                    Subtitle = subtitle,
                    Text = text,
                    Images = new List<CardImage>() { cardImage },
                    Buttons = new List<CardAction>() { cardAction, cardAction2 },
                };

                return heroCard.ToAttachment();
            }
            else
            {
                var heroCard = new HeroCard
                {
                    Title = title,
                    Subtitle = subtitle,
                    Text = text,
                    Images = new List<CardImage>() { cardImage },
                    Buttons = new List<CardAction>() { cardAction },
                };

                return heroCard.ToAttachment();
            }
        }


        public static DataTable GetWiadomosci()
        {
            DataTable dt = new DataTable();

            try
            {
                SqlConnection sqlConnection1 = new SqlConnection("Server=tcp:plps.database.windows.net,1433;Initial Catalog=PLPS;Persist Security Info=False;User ID=tomasoft;Password=Tomason18,;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
                SqlCommand cmd = new SqlCommand();

                cmd.CommandText = "SELECT * FROM [dbo].[Wiadomosci" + BaseDB.appName + "]";
                cmd.CommandType = CommandType.Text;
                cmd.Connection = sqlConnection1;

                sqlConnection1.Open();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
                sqlConnection1.Close();
                return dt;
            }
            catch
            {
                BaseDB.AddToLog("Błąd pobierania wiadomości");
                return null;
            }
        }


        public static DataTable GetUser()
        {
            DataTable dt = new DataTable();

            try
            {
                SqlConnection sqlConnection1 = new SqlConnection("Server=tcp:plps.database.windows.net,1433;Initial Catalog=PLPS;Persist Security Info=False;User ID=tomasoft;Password=Tomason18,;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
                SqlCommand cmd = new SqlCommand();

                cmd.CommandText = "SELECT * FROM [dbo].[User" + BaseDB.appName + "] where flgDeleted=0";
                cmd.CommandType = CommandType.Text;
                cmd.Connection = sqlConnection1;

                sqlConnection1.Open();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
                sqlConnection1.Close();
                return dt;
            }
            catch
            {
                BaseDB.AddToLog("Błąd pobierania użytkowników");
                return null;
            }
        }
        public static DataTable GetUserById(long userId)
        {
            DataTable dt = new DataTable();

            try
            {
                SqlConnection sqlConnection1 = new SqlConnection("Server=tcp:plps.database.windows.net,1433;Initial Catalog=PLPS;Persist Security Info=False;User ID=tomasoft;Password=Tomason18,;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
                SqlCommand cmd = new SqlCommand();

                cmd.CommandText = "SELECT * FROM [dbo].[User" + BaseDB.appName + "] where flgDeleted=0 and userId="+userId;
                cmd.CommandType = CommandType.Text;
                cmd.Connection = sqlConnection1;

                sqlConnection1.Open();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
                sqlConnection1.Close();
                return dt;
            }
            catch
            {
                BaseDB.AddToLog("Błąd pobierania użytkowników");
                return null;
            }
        }

    }
}