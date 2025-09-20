using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppRestorer;

public class Constants
{
    public static Uri FavEnabled = new Uri(@"Assets\FavoriteIcon9.png", UriKind.Relative);
    public static Uri FavDisabled = new Uri(@"Assets\FavoriteIcon10.png", UriKind.Relative);
    
    // EventBus
    public static string EB_ToModel = "Model_Message";
    public static string EB_ToWindow = "Window_Message";
    public static string EB_ToSettings = "Settings_Message";
}
