﻿using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using NLog;
using RemoteFork.Network;
using RemoteFork.Plugins;
using RemoteFork.Server;
using HttpListenerRequest = Unosquare.Net.HttpListenerRequest;
using HttpListenerResponse = Unosquare.Net.HttpListenerResponse;

namespace RemoteFork.Requestes {
    internal class PluginRequestHandler : BaseRequestHandler {
        private static readonly ILogger Log = LogManager.GetLogger("TestRequestHandler", typeof(TestRequestHandler));

        internal static readonly string ParamPluginKey = "plugin";

        internal static readonly Regex PluginParamRegex = new Regex($@"{ParamPluginKey}(\w+)[\\]?", RegexOptions.Compiled);

        public override void Handle(HttpListenerRequest request, HttpListenerResponse response) {
            var pluginKey = ParsePluginKey(request);

            if (!string.IsNullOrEmpty(pluginKey)) {
                var plugin = PluginManager.Instance.GetPlugin(pluginKey);

                if (plugin != null) {
                    Log.Debug("Execute: {0}", plugin.Name);

                    var pluginResponse = plugin.Instance.GetList(new PluginContext(pluginKey, request, request.QueryString));

                    if (pluginResponse != null) {
                        if (pluginResponse.source != null) {
                            Log.Debug(
                                "Plugin Playlist.source not null! Write to response Playlist.source and ignore other methods. Plugin: {0}",
                                pluginKey);
                            HTTPUtility.WriteResponse(response, pluginResponse.source);
                        } else {
                            HTTPUtility.WriteResponse(response, ResponseSerializer.ToXml(pluginResponse));
                        }
                    } else {
                        Log.Warn("Plugin Playlist is null. Plugin: {0}", pluginKey);

                        HTTPUtility.WriteResponse(response, HttpStatusCode.NotFound, $"Plugin Playlist is null. Plugin: {pluginKey}");
                    }
                } else {
                    Log.Warn("Plugin Not Found. Plugin: {0}", pluginKey);

                    HTTPUtility.WriteResponse(response, HttpStatusCode.NotFound, $"Plugin Not Found. Plugin: {pluginKey}");
                }
            } else {
                Log.Warn("Plugin is not defined in request. Plugin: {0}", pluginKey);

                HTTPUtility.WriteResponse(response, HttpStatusCode.NotFound, $"Plugin is not defined in request. Plugin: {pluginKey}");
            }
        }

        private static string ParsePluginKey(HttpListenerRequest request) {
            string pluginParam = request.QueryString.GetValues(string.Empty)?.FirstOrDefault(s => PluginParamRegex.IsMatch(s ?? string.Empty));

            var pluginParamMatch = PluginParamRegex.Match(pluginParam ?? string.Empty);

            return pluginParamMatch.Success ? pluginParamMatch.Groups[1].Value : string.Empty;
        }

        internal static string CreatePluginUrl(HttpListenerRequest request, string pluginName, NameValueCollection parameters = null) {
            var query = new NameValueCollection() {
                {string.Empty, string.Concat(ParamPluginKey, pluginName, Path.DirectorySeparatorChar, ".xml")}
            };

            string url = request.Url.OriginalString.Substring(7);
            query.Add("host", url.Substring(0, url.IndexOf("/")));
            if (parameters != null) {
                foreach (var parameter in parameters.AllKeys) {
                    query.Add(parameter, parameters[parameter]);
                }
            }

            return CreateUrl(request, RootRequestHandler.TreePath, query);
        }
    }
}