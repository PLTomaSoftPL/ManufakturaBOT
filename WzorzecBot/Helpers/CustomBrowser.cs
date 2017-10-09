using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using System.Windows.Forms;

namespace GksKatowiceBot.Helpers
{
    public class CustomBrowser
    {
        public CustomBrowser()
        {
            //
            // TODO: Add constructor logic here
            //
        }
        private string GeneratedSource { get; set; }
        public void GetWebpage(string url)
        {
            WebBrowser wb = new WebBrowser();
            wb.ScriptErrorsSuppressed = true;
            wb.Navigate(url);

            wb.DocumentCompleted +=
                new WebBrowserDocumentCompletedEventHandler(
                    wb_DocumentCompleted);

            while (wb.ReadyState != WebBrowserReadyState.Complete)
                Application.DoEvents();

            //Added this line, because the final HTML takes a while to show up
            GeneratedSource = wb.Document.Body.InnerHtml;

            wb.Dispose();
        }

        private void wb_DocumentCompleted(object sender,
            WebBrowserDocumentCompletedEventArgs e)
        {
            WebBrowser wb = (WebBrowser)sender;
            GeneratedSource = wb.Document.Body.InnerHtml;
        }
    }
}