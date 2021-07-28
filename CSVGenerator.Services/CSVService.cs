using CSVGenerator.Services.DataTransferObjects;
using CSVGenerator.Services.Interfaces;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CSVGenerator.Services
{
    public class CSVService : ICSVService
    {
        public static string GetDescription(object enumValue)
        {
            var description = enumValue.ToString();
            var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());

            if (fieldInfo != null)
            {
                var attrs = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), true);
                if (attrs != null && attrs.Length > 0)
                {
                    description = ((DescriptionAttribute)attrs[0]).Description;
                }
            }

            return description;
        }

        static Dictionary<Type, string> PropertyMapper = new Dictionary<Type, string>()
        {
            { typeof(DateTime), "datetime" },
            { typeof(DateTime?), "datetime" },
            { typeof(string), "nvarchar" },
            { typeof(int), "int" },
            { typeof(int?), "int" },
            { typeof(decimal), "decimal" },
            { typeof(decimal?), "decimal" },
            { typeof(double), "double" },
            { typeof(double?), "double" },
            { typeof(bool), "bit" }
        };
        public async Task<(string path, string fileName)> GenerateEnumCsvAsync(string projPath, RequestDto request)
        {
            var ProjectName = projPath.Split('\\')[^1];
            var dlls = new List<string>();

            request.Prefixes?.ForEach(prefix =>
            {
                var mainList = Directory.GetFiles(projPath, $"{prefix}*.dll", SearchOption.AllDirectories);
                var uniqueDLLs = mainList.Where(x => request.IgnoreAssemblyKeywords.All(y => !x.ToLower().Contains(y.ToLower()))).Select(x => new
                {
                    Path = x,
                    FileName = x.Split('\\', StringSplitOptions.RemoveEmptyEntries)[^1],
                    Size = new FileInfo(x).Length
                }).GroupBy(x => x.FileName).Select(g => g.OrderByDescending(x => x.Size).FirstOrDefault().Path);

                dlls.AddRange(uniqueDLLs);
            });

            foreach (var dll in dlls)
            {
                try
                {
                    var DLL = Assembly.LoadFrom(dll);

                    // ENUMS
                    var enums = DLL.GetExportedTypes().Where(type => type.IsEnum);
                    foreach (Type en in enums)
                    {
                        var csvObj = new List<object>();
                        foreach (var value in Enum.GetValues(en))
                        {
                            csvObj.Add(new
                            {
                                Name = Enum.GetName(en, value),
                                Value = (int)value,
                                Notes = GetDescription(value)
                            });
                        }
                        var csvPath = Path.Combine(Path.GetTempPath(), ProjectName, "Enums", $"{en.Namespace}", $"{en.Name}.csv");

                        FileInfo file = new FileInfo(csvPath);

                        if (!file.Directory.Exists)
                        {
                            Directory.CreateDirectory(file.DirectoryName);
                        }

                        using (var writer = new StreamWriter(csvPath))
                        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                        {
                            await csv.WriteRecordsAsync(csvObj);
                        }
                    }

                    //Entities
                    var entities = DLL.GetExportedTypes().Where(type => type.Namespace.Contains("Entities"));
                    foreach (Type entity in entities)
                    {
                        var csvObj = new List<object>();
                        
                        foreach (var prop in entity.GetProperties())
                        {
                            if(!prop.PropertyType.IsClass || prop.PropertyType == typeof(string))
                                csvObj.Add(new
                                {
                                    Column = prop.Name,
                                    DataType = prop.PropertyType.IsEnum ? "int" : (PropertyMapper.TryGetValue(prop.PropertyType, out string dt) ? dt : prop.PropertyType.Name),
                                    Description = prop.PropertyType.IsEnum ? prop.PropertyType.Name : string.Empty,
                                    ReferencedTable = string.Empty,
                                    AllowedValues = Nullable.GetUnderlyingType(prop.PropertyType) != null ? "Nullable" : string.Empty
                                });
                        }
                        var csvPath = Path.Combine(Path.GetTempPath(), ProjectName, "Entities", $"{entity.Namespace}", $"{entity.Name}.csv");

                        FileInfo file = new FileInfo(csvPath);
                        if (!file.Directory.Exists)
                        {
                            Directory.CreateDirectory(file.DirectoryName);
                        }

                        using (var writer = new StreamWriter(csvPath))
                        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                        {
                            await csv.WriteRecordsAsync(csvObj);
                        }
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            string zipPath = Path.Combine(Path.GetTempPath(), $"{ProjectName}.zip");
            ZipFile.CreateFromDirectory(Path.Combine(Path.GetTempPath(), ProjectName), zipPath);
            return (zipPath, $"{ProjectName}.zip");
        }
    }
}
