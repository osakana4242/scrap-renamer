namespace ScrapRenamer.Localization.Strings {
    using System;
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("hetima.resx-designer", "0.3.2")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Strings {

        private static global::System.Resources.ResourceManager resourceMan;

        private static global::System.Globalization.CultureInfo resourceCulture;

        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Strings() {
        }

        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("ScrapRenamer.Localization.Strings", typeof(Strings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }

        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }

        public static string AppName {
            get {
                return ResourceManager.GetString("AppName", resourceCulture);
            }
        }

        public static string Button_Apply {
            get {
                return ResourceManager.GetString("Button_Apply", resourceCulture);
            }
        }

        public static string Button_Clear {
            get {
                return ResourceManager.GetString("Button_Clear", resourceCulture);
            }
        }

        public static string Button_Sort {
            get {
                return ResourceManager.GetString("Button_Sort", resourceCulture);
            }
        }

        public static string Dialog_Settings_Title {
            get {
                return ResourceManager.GetString("Dialog_Settings_Title", resourceCulture);
            }
        }

        public static string DropFilesHere {
            get {
                return ResourceManager.GetString("DropFilesHere", resourceCulture);
            }
        }

        public static string Error_FileNotFound {
            get {
                return ResourceManager.GetString("Error_FileNotFound", resourceCulture);
            }
        }

        public static string Menu_File {
            get {
                return ResourceManager.GetString("Menu_File", resourceCulture);
            }
        }

        public static string Menu_File_Open {
            get {
                return ResourceManager.GetString("Menu_File_Open", resourceCulture);
            }
        }

        public static string Menu_File_Quit {
            get {
                return ResourceManager.GetString("Menu_File_Quit", resourceCulture);
            }
        }

        public static string Menu_Help {
            get {
                return ResourceManager.GetString("Menu_Help", resourceCulture);
            }
        }

        public static string Menu_Help_About {
            get {
                return ResourceManager.GetString("Menu_Help_About", resourceCulture);
            }
        }

        public static string Menu_Settings {
            get {
                return ResourceManager.GetString("Menu_Settings", resourceCulture);
            }
        }

        public static string Message_RenameCompleted {
            get {
                return ResourceManager.GetString("Message_RenameCompleted", resourceCulture);
            }
        }

    }
}
