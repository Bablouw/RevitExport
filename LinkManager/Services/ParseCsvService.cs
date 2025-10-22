using LinkManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkManager.Services
{
    public class ParseCsvService
    {
        public List<LinkModel> ParseCsv(string path)
        {
            List<LinkModel> parsedLinks = new List<LinkModel>();

            try
            {
                // Читаем все строки из CSV-файла
                string[] lines = File.ReadAllLines(path);

                // Пропускаем заголовок (первая строка)
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue; // Пропускаем пустые строки

                    // Разделяем строку по запятой
                    string[] columns = line.Split(';');

                    // Проверяем, что строка содержит ровно 2 столбца
                    if (columns.Length == 2)
                    {
                        LinkModel model = new LinkModel
                        {
                            linkName = columns[0].Trim(),
                            workShareName = columns[1].Trim()
                        };
                        parsedLinks.Add(model);
                    }
                    else
                    {
                        // Можно записать в лог или выбросить исключение, если строка некорректна
                        // Для простоты просто пропускаем
                        continue;
                    }
                }
            }
            catch (FileNotFoundException)
            {
                LogService.LogError($"Файл по пути {path} не найден.");
            }
            catch (IOException ex)
            {
                LogService.LogError($"Ошибка чтения CS: {ex}");
            }
            catch (Exception ex)
            {
                LogService.LogError($"Ошибка при обработки CSV:{ex}");
            }

            return parsedLinks;
        }
    }
}
