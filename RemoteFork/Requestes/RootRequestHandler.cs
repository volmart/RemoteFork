﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using NLog;
using RemoteFork.Network;
using RemoteFork.Plugins;
using RemoteFork.Properties;
using RemoteFork.Server;
using Unosquare.Net;

namespace RemoteFork.Requestes {
    internal class RootRequestHandler : BaseRequestHandler {
        private static readonly ILogger Log = LogManager.GetLogger("RootRequestHandler", typeof(RootRequestHandler));

        internal static readonly string TreePath = "/treeview";

        internal static readonly string RootPath = "/";

        public override void Handle(HttpListenerRequest request, HttpListenerResponse response) {
            var result = new List<Item>();

            if (Settings.Default.DlnaFilterType == 1) {
                if (Settings.Default.DlnaDirectories != null) {
                    foreach (var directory in Settings.Default.DlnaDirectories) {
                        if (Directory.Exists(directory)) {
                            result.Add(DlnaDirectoryRequestHandler.CreateDirectoryItem(request, directory));

                            Log.Debug($"Filtering directory: {directory}");
                        }
                    }
                }
            } else {
                var drives = DriveInfo.GetDrives();

                foreach (var drive in drives.Where(i => Tools.CheckAccessPath(i.Name))) {
                    if (drive.IsReady) {
                        string mainText =
                            $"{drive.Name} ({Tools.FSize(drive.AvailableFreeSpace)} свободно из {Tools.FSize(drive.TotalSize)})";
                        string subText = $"<br>Метка диска: {drive.VolumeLabel}<br>Тип носителя: {drive.DriveType}";

                        result.Add(new Item {
                            Name = mainText + subText,
                            Link = CreateUrl(
                                request,
                                TreePath,
                                new NameValueCollection() {
                                    {string.Empty, new Uri(drive.Name).AbsoluteUri}
                                }),
                            Type = ItemType.DIRECTORY
                        });

                        Log.Debug($"Drive: {mainText}{subText}");
                    }
                }
            }

            if ((Settings.Default.UserUrls != null) && (Settings.Default.UserUrls.Count > 0)) {
                result.Add(
                    new Item {
                        Name = "Пользовательские ссылки",
                        Link = CreateUrl(request, TreePath,
                            new NameValueCollection() {
                                {string.Empty, UserUrlsRequestHandler.ParamUrls}
                            }),
                        Type = ItemType.DIRECTORY
                    }
                );

                Log.Debug("User urls: {0}", Settings.Default.UserUrls.Count);
            }

            foreach (var plugin in PluginManager.Instance.GetPlugins()) {
                result.Add(
                    new Item {
                        Name = plugin.Value.Name,
                        Link = PluginRequestHandler.CreatePluginUrl(request, plugin.Key),
                        ImageLink = plugin.Value.ImageLink,
                        Type = ItemType.DIRECTORY
                    }
                );

                Log.Debug("Plugin: {0}", plugin.Value.Name);
            }

            HTTPUtility.WriteResponse(response, ResponseSerializer.ToM3U(result.ToArray()));
        }
    }
}
