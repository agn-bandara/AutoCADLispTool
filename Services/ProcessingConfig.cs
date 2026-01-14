using System;
using System.IO;
using System.Xml.Serialization;

namespace AutoCADLispTool.Services
{
    /// <summary>
    /// Configurable processing parameters for drawing operations
    /// </summary>
    public class ProcessingConfig
    {
        public int DocumentActivationDelayMs { get; set; } = 50;
        public int LispLoadDelayMs { get; set; } = 150;
        public int CommandExecutionDelayMs { get; set; } = 150;
        public int SaveDelayMs { get; set; } = 300;

        public ProcessingConfig()
        {
        }

        /// <summary>
        /// Load configuration from file if it exists, otherwise return default
        /// </summary>
        public static ProcessingConfig LoadFromFile(string path)
        {
            if (!File.Exists(path))
            {
                return new ProcessingConfig();
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ProcessingConfig));
                using (FileStream fs = new FileStream(path, FileMode.Open))
                {
                    return (ProcessingConfig)serializer.Deserialize(fs);
                }
            }
            catch
            {
                return new ProcessingConfig();
            }
        }

        /// <summary>
        /// Save configuration to file
        /// </summary>
        public void SaveToFile(string path)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ProcessingConfig));
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    serializer.Serialize(fs, this);
                }
            }
            catch
            {
                // Ignore save errors
            }
        }
    }
}
