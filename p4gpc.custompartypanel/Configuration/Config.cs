using p4gpc.custompartypanel.Configuration.Implementation;
using System.ComponentModel;

namespace p4gpc.custompartypanel.Configuration
{
    public class Config : Configurable<Config>
    {
        /*
            User Properties:
                - Please put all of your configurable properties here.
                - Tip: Consider using the various available attributes https://stackoverflow.com/a/15051390/11106111
        
            By default, configuration saves as "Config.json" in mod folder.    
            Need more config files/classes? See Configuration.cs
        */

        [DisplayName("Debug Mode")]
        [Description("Logs additional information to the console that is useful for debugging.")]
        public bool DebugEnabled { get; set; } = false;

        [DisplayName("Epic Gamer RGB")]
        [Description("Turns the party panels into something fit for a real gamer. The rgb transition is based on the set fg and bg colours so try changing them for the best effect.")]
        public bool RgbMode { get; set; } = false;

        // Protag
        [DisplayName("Protagonist Foreground Colour")]
        [Description("The colour of the foreground of the Protagonist's party panel")]
        public Colour ProtagonistFgColour { get; set; } = new Colour(131, 134, 139);

        [DisplayName("Protagonist Background Colour")]
        [Description("The colour of the background of the Protagonist's party panel")]
        public Colour ProtagonistBgColour { get; set; } = new Colour(85, 87, 91);

        // Yosuke
        [DisplayName("Yosuke Foreground Colour")]
        [Description("The colour of the foreground of Yosuke's party panel")]
        public Colour YosukeFgColour { get; set; } = new Colour(210, 142, 87);

        [DisplayName("Yosuke Background Colour")]
        [Description("The colour of the background of Yosuke's party panel")]
        public Colour YosukeBgColour { get; set; } = new Colour(142, 90, 49);

        // Chie
        [DisplayName("Chie Foreground Colour")]
        [Description("The colour of the foreground of Chie's party panel")]
        public Colour ChieFgColour { get; set; } = new Colour(108, 169, 77);

        [DisplayName("Chie Background Colour")]
        [Description("The colour of the background of Chie's party panel")]
        public Colour ChieBgColour { get; set; } = new Colour(67, 112, 44);

        // Yukiko
        [DisplayName("Yukiko Foreground Colour")]
        [Description("The colour of the foreground of Yukiko's party panel")]
        public Colour YukikoFgColour { get; set; } = new Colour(210, 56, 49);

        [DisplayName("Yukiko Background Colour")]
        [Description("The colour of the background of Yukiko's party panel")]
        public Colour YukikoBgColour { get; set; } = new Colour(147, 32, 27);

        // Kanji
        [DisplayName("Kanji Foreground Colour")]
        [Description("The colour of the foreground of Kanji's party panel")]
        public Colour KanjiFgColour { get; set; } = new Colour(208, 185, 127);

        [DisplayName("Kanji Background Colour")]
        [Description("The colour of the background of Kanji's party panel")]
        public Colour KanjiBgColour { get; set; } = new Colour(137, 120, 77);

        // Naoto
        [DisplayName("Naoto Foreground Colour")]
        [Description("The colour of the foreground of Naoto's party panel")]
        public Colour NaotoFgColour { get; set; } = new Colour(70, 70, 131);

        [DisplayName("Naoto Background Colour")]
        [Description("The colour of the background of Naoto's party panel")]
        public Colour NaotoBgColour { get; set; } = new Colour(46, 46, 91);

        // Teddie
        [DisplayName("Teddie Foreground Colour")]
        [Description("The colour of the foreground of Teddie's party panel")]
        public Colour TeddieFgColour { get; set; } = new Colour(247, 170, 148);

        [DisplayName("Teddie Background Colour")]
        [Description("The colour of the background of Teddie's party panel")]
        public Colour TeddieBgColour { get; set; } = new Colour(166, 109, 92);
    }
}
