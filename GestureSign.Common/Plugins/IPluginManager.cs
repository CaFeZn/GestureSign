using System;

namespace GestureSign.Common.Plugins
{
	public interface IPluginManager
	{
		IPluginInfo FindPluginByClassAndFilename(string PluginClass, string PluginFilename);
		bool PluginExists(string PluginClass, string PluginFilename);
		bool LoadPlugins(IHostControl host);
		bool RepeatLastCommand(PointInfo actionPoint);
		IPluginInfo[] Plugins { get; }
	}
}
