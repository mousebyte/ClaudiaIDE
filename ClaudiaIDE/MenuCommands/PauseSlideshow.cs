using System;
using System.ComponentModel.Design;
using ClaudiaIDE.Loaders;
using ClaudiaIDE.Settings;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace ClaudiaIDE.MenuCommands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class PauseSlideshow
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int PauseCommandId = 0x0110;

        public const int ResumeCommandId = 0x0120;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("f0ffaf7c-8feb-40d2-b898-1acfe50e1d6b");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage _package;

        /// <summary>
        /// Initializes a new instance of the <see cref="PauseSlideshow"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private PauseSlideshow(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var pauseCommandID = new CommandID(CommandSet, PauseCommandId);
            var resumeCommandID = new CommandID(CommandSet, ResumeCommandId);
            var pauseMenuItem = new OleMenuCommand(Execute, pauseCommandID);
            var resumeMenuItem = new OleMenuCommand(Execute, resumeCommandID);
            pauseMenuItem.BeforeQueryStatus += (sender, args) =>
            {
                if (!(sender is OleMenuCommand cmd)) return;
                UpdatePauseVisibility(cmd);
            };
            resumeMenuItem.BeforeQueryStatus += (sender, args) =>
            {
                if (!(sender is OleMenuCommand cmd)) return;
                UpdateResumeVisibility(cmd);
            };
            commandService.AddCommand(pauseMenuItem);
            commandService.AddCommand(resumeMenuItem);
        }

        private static bool? IsPaused
        {
            get
            {
                if (ImageProvider.Instance.Loader is SlideshowImageLoader s)
                    return s.Paused;
                return null;
            }
        }

        private static void UpdatePauseVisibility(MenuCommand cmd)
        {
            var paused = IsPaused;
            cmd.Enabled = !paused ?? false;
            cmd.Visible = !paused ?? true;
        }

        private static void UpdateResumeVisibility(MenuCommand cmd)
        {
            var paused = IsPaused ?? false;
            cmd.Enabled = paused;
            cmd.Visible = paused;
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static PauseSlideshow Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IAsyncServiceProvider ServiceProvider => _package;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in PauseSlideshow's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService =
                await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new PauseSlideshow(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (ImageProvider.Instance.Loader is SlideshowImageLoader slideshow)
                slideshow.Paused = !slideshow.Paused;
        }
    }
}