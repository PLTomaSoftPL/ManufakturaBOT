using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Framework.Builder.Witai;
using Microsoft.Bot.Framework.Builder.Witai.Dialogs;
using Microsoft.Bot.Framework.Builder.Witai.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Bot.Connector;

namespace GksKatowiceBot.Helpers
{
    [Serializable]
    [WitModel("E3WX66GBTW3UVX55755JCCB5JU3FG43H")]
    public class WeatherDialog : WitDialog<object>
    {
        public override Task StartAsync(IDialogContext context)
        {
            return base.StartAsync(context);
        }
        [WitAction("Test2")]
        public async Task GetForecast(IDialogContext context, WitResult result)
        {
            //adding location to context
            this.WitContext["location"] = result.Entities["location"][0].Value;

        }
        protected override Task MessageReceived(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            return base.MessageReceived(context, item);
        }
    }
}