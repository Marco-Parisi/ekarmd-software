using System;
using System.IO;
using System.Xml;
using MuonDetectorReader.Models;
using MuonDetectorReader.Services.Interfaces;

namespace MuonDetectorReader.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly string _settingFolderPath;
        private readonly string _settingFilePath;

        public SettingsService()
        {
            _settingFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MuonDetectorReader");
            _settingFilePath = Path.Combine(_settingFolderPath, "Settings.xml");
        }

        public AppSettings Load()
        {
            var settings = new AppSettings
            {
                ReferencePressure = 1013.25,
                Beta = 0,
                SigmaBeta = 0,
                KT = -0.00170065228125, // Valido solo per EKAR, stimato dai dati 2024_09 - 2025_11
                SigmaKT = 1.3346453895E-18,
                IsSidebarOnRight = false,
                Theme = "Light"
            };

            try
            {
                if (File.Exists(_settingFilePath))
                {
                    XmlDocument xml = new XmlDocument();
                    xml.Load(_settingFilePath);
                    XmlNode root = xml.SelectSingleNode("Settings");

                    if (root != null)
                    {
                        var refPressNode = root.SelectSingleNode("RefPressure");
                        if (refPressNode != null && double.TryParse(refPressNode.InnerText, out double p0))
                        {
                            settings.ReferencePressure = p0;
                        }

                        var betaNode = root.SelectSingleNode("Beta");
                        if (betaNode != null && double.TryParse(betaNode.InnerText, out double b))
                        {
                            settings.Beta = b;
                        }

                        var sigmaNode = root.SelectSingleNode("SigmaBeta");
                        if (sigmaNode != null && double.TryParse(sigmaNode.InnerText, out double sb))
                        {
                            settings.SigmaBeta = sb;
                        }

                        var ktNode = root.SelectSingleNode("KT");
                        if (ktNode != null && double.TryParse(ktNode.InnerText, out double kt))
                        {
                            settings.KT = kt;
                        }

                        var sigmaKtNode = root.SelectSingleNode("SigmaKT");
                        if (sigmaKtNode != null && double.TryParse(sigmaKtNode.InnerText, out double skt))
                        {
                            settings.SigmaKT = skt;
                        }

                        var sidebarNode = root.SelectSingleNode("IsSidebarOnRight");
                        if (sidebarNode != null && bool.TryParse(sidebarNode.InnerText, out bool sor))
                        {
                            settings.IsSidebarOnRight = sor;
                        }

                        var dottedNode = root.SelectSingleNode("IsSeriesDottedChecked");
                        if (dottedNode != null && bool.TryParse(dottedNode.InnerText, out bool dotted))
                        {
                            settings.IsSeriesDottedChecked = dotted;
                        }

                        var themeNode = root.SelectSingleNode("Theme");
                        if (themeNode != null && !string.IsNullOrWhiteSpace(themeNode.InnerText))
                        {
                            settings.Theme = themeNode.InnerText.Trim();
                        }

                        var langNode = root.SelectSingleNode("Language");
                        if (langNode != null && !string.IsNullOrWhiteSpace(langNode.InnerText))
                        {
                            settings.Language = langNode.InnerText.Trim();
                        }
                    }
                    else
                    {
                        Save(settings);
                    }
                }
                else
                {
                    Save(settings);
                }
            }
            catch
            {
                Save(settings);
            }

            return settings;
        }

        public void Save(AppSettings settings)
        {
            try
            {
                if (!Directory.Exists(_settingFolderPath))
                {
                    Directory.CreateDirectory(_settingFolderPath);
                }

                XmlDocument xml = new XmlDocument();
                XmlElement root = xml.CreateElement("Settings");

                XmlElement refPressNode = xml.CreateElement("RefPressure");
                refPressNode.InnerText = settings.ReferencePressure.ToString();
                root.AppendChild(refPressNode);

                XmlElement betaNode = xml.CreateElement("Beta");
                betaNode.InnerText = settings.Beta.ToString();
                root.AppendChild(betaNode);

                XmlElement sigmaNode = xml.CreateElement("SigmaBeta");
                sigmaNode.InnerText = settings.SigmaBeta.ToString();
                root.AppendChild(sigmaNode);

                XmlElement ktNode = xml.CreateElement("KT");
                ktNode.InnerText = settings.KT.ToString();
                root.AppendChild(ktNode);

                XmlElement sigmaKtNode = xml.CreateElement("SigmaKT");
                sigmaKtNode.InnerText = settings.SigmaKT.ToString();
                root.AppendChild(sigmaKtNode);

                XmlElement sidebarNode = xml.CreateElement("IsSidebarOnRight");
                sidebarNode.InnerText = settings.IsSidebarOnRight.ToString();
                root.AppendChild(sidebarNode);

                XmlElement dottedNode = xml.CreateElement("IsSeriesDottedChecked");
                dottedNode.InnerText = settings.IsSeriesDottedChecked.ToString();
                root.AppendChild(dottedNode);

                XmlElement themeNode = xml.CreateElement("Theme");
                themeNode.InnerText = settings.Theme ?? "Light";
                root.AppendChild(themeNode);

                XmlElement langNode = xml.CreateElement("Language");
                langNode.InnerText = settings.Language ?? "";
                root.AppendChild(langNode);

                xml.AppendChild(root);
                xml.Save(_settingFilePath);
            }
            catch
            {
                // Ignore save errors
            }
        }
    }
}
