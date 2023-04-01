﻿using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Tasler.RenewedPowerCommands.Common;
using Tasler.RenewedPowerCommands.Extensions;
using Tasler.RenewedPowerCommands.Linq;
using Tasler.RenewedPowerCommands.OptionPages;
using Tasler.RenewedPowerCommands.Shell;

namespace Tasler.RenewedPowerCommands.Commands
{
	[Guid("DAF452A2-2D1F-4D11-B477-97C2F71809D1")]
	[DisplayName("Remove and Sort Usings")]
	internal class RemoveSortUsingsCommand : DynamicCommand
	{
		public RemoveSortUsingsCommand(IServiceProvider serviceProvider)
			: base(serviceProvider,
				  RemoveSortUsingsCommand.OnExecute,
				  new CommandID(typeof(RemoveSortUsingsCommand).GUID, c_cmdidRemoveSortUsingsCommand))
		{
		}

		protected override bool CanExecute(OleMenuCommand command)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (Package.OptionsPage.RemoveAndSortUsingsCommand != DisabledEnabled.Disabled)
			{
				Project project = Dte.SelectedItems.Item(1).Project;
				if (project == null)
				{
					return RemoveSortUsingsCommand.IsAtLeastOneCSharpProject();
				}
				if (project.IsKind(VSLangProj.PrjKind.prjKindCSharpProject))
				{
					return true;
				}
			}
			return false;
		}

		private static void OnExecute(object sender, EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			Project project = Dte.SelectedItems.Item(1).Project;
			if (project?.Kind == VSLangProj.PrjKind.prjKindCSharpProject)
			{
				RemoveSortUsingsCommand.ProcessProject(project);
				return;
			}
			foreach (var proj in new ProjectIterator(Dte.Solution).Where(p => p.IsKind(VSLangProj.PrjKind.prjKindCSharpProject)))
			{
				RemoveSortUsingsCommand.ProcessProject(proj);
			}
		}

		private static bool IsAtLeastOneCSharpProject()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			return new ProjectIterator(Dte.Solution).Any(p => p.IsKind(VSLangProj.PrjKind.prjKindCSharpProject));
		}

		private static void ProcessProject(Project project)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (project != null)
			{
				if (project.Compile() != 0)
				{
					new ErrorListWindow(ServiceProvider).Show();
					return;
				}

				var alreadyOpenFiles = new RunningDocumentTable(ServiceProvider).Select(info => info.Moniker).ToList();

				foreach (var item in new ProjectItemIterator(project.ProjectItems).Where(i => i.GetFileCodeModel() != null))
				{
					var fileName = item.get_FileNames(1);
					Window window = Dte.OpenFile(EnvDTE.Constants.vsViewKindTextView, fileName);
					window.Activate();
					try
					{
						Dte.ExecuteCommand("Edit.RemoveAndSort", string.Empty);
					}
					catch (COMException)
					{
					}

					if (alreadyOpenFiles.SingleOrDefault(f => f.Equals(f, StringComparison.OrdinalIgnoreCase)) != null)
					{
						Dte.ActiveDocument.Save(fileName);
						return;
					}

					window.Close(vsSaveChanges.vsSaveChangesYes);
				}
			}
		}

		public const int c_cmdidRemoveSortUsingsCommand = 0xDBE;
	}
}
