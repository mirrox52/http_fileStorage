using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace http_file_server
{
    public class Http_server
    {
        public Http_server()
        {

        }

        public void Start()
        {
            var _listener = new HttpListener(); // слушаем порт
            try
            {
                _listener.Prefixes.Add("http://*:3000/"); // за пользователя добавляет локалхост и порт 3000
                _listener.Start(); // запускаем listener
                Console.WriteLine("Listening...");
                while (true) // бесконечный цикл для принятия команд
                {
                    HttpListenerContext context = _listener.GetContext(); // получает информацию, которую послали
                    var request = context.Request.HttpMethod;
                    Console.WriteLine(request);
                    HttpListenerResponse response = context.Response; // получение ответа (код)
                    try
                    {
                        switch (request) // запросы в postman

                        {
                            case "GET":
                                {
                                    getCommand(context.Request, response);
                                    break;
                                }
                            case "PUT": 
                                {
                                    putCommand(context.Request, response);
                                    break;
                                }
                            case "HEAD": 
                                {
                                    headCommand(context.Request, response);
                                    break;
                                }
                            case "DELETE": 
                                {
                                    deleteCommand(context.Request, response);
                                    break;
                                }

                        }
                    }
                    catch
                    {
                        response.StatusCode = 404;
                    }


                }
            }
            finally
            {

                _listener.Stop();
            }
        }


        public void getCommand(HttpListenerRequest request, HttpListenerResponse response)
        {
            Stream output = response.OutputStream; // поток для прослушки ответа

           
            var writer = new StreamWriter(output); // с помощью writer отправляем содержимое файла или содержимое каталога

            string fullPath = Directory.GetCurrentDirectory() + request.RawUrl; // полный путь к файлу (наша папка + то, что после локлхост)

            try
            {
                if (File.Exists(fullPath))
                {
                    try
                    {
                        using (var file = File.Open(fullPath, FileMode.Open)) //отображение файла
                        {
                            file.CopyTo(output);
                            file.Close();
                        }


                    }
                    catch (Exception ex)
                    {
                        response.StatusCode = 500;
                        writer.Write($"Local error happened: {ex.Message}.");
                    }
                }

                if (!File.Exists(fullPath)) // если это не файл
                {
                    try
                    {
                    
                    
                    var result = new List<object>(); // создание списка объектов
                        foreach (var entry in Directory.GetDirectories(fullPath).Concat(Directory.GetFiles(fullPath))) // запись содержимого директории в список
                        {

                            result.Add(new
                            {
                                name = entry.Substring(Directory.GetCurrentDirectory().Length), // берем подстроку после локалхоста
                                creationTime = Directory.GetCreationTime(entry)
                            });
                        }

                        writer.Write(JsonConvert.SerializeObject(result)); // отображение содержимого директории
                        writer.Flush(); // очищение writer
                    }
                    catch (Exception ex)
                    {
                        response.StatusCode = 404;
                        writer.Write($"Local error happened: {ex.Message}.");
                    }
                }
         
            }         
            finally
            {
                output.Close();
                writer.Dispose();
            }
        }

        public void putCommand(HttpListenerRequest request, HttpListenerResponse response) // загружает на сервер файлы
        {
            try
            {

                var head = request.Headers["x-copy-from"]; // если в заголовке это, то мы копируем файл
                string fullPath = Directory.GetCurrentDirectory() + request.RawUrl; // полный путь

                if (head == null) // если не копирование, то создание
                {

                    var catalog = Path.GetDirectoryName(fullPath); // получаем имя директории

                    if (!Directory.Exists(catalog)) // если директории не существует, то мы ее создаем
                    {
                        Directory.CreateDirectory(catalog);
                    }

                    using (var newFile = new FileStream(fullPath, FileMode.Create)) // создаем файл // освобождение ресурсов
                    {
                        request.InputStream.CopyTo(newFile); // запись в файл который мы создаем

                    }

                    return;
                }
                else // копирование
                {
                    string[] list = head.Split('/'); 
                    try
                    {
                        File.Copy(Directory.GetCurrentDirectory() + head, fullPath + '/' + list.Last()); // откуда куда
                    }
                    catch
                    {
                        response.StatusCode = 501;
                    }

                }

            }
            finally
            {
                response.OutputStream.Close();
            }


        }

        public void headCommand(HttpListenerRequest request, HttpListenerResponse response) // отображение header'a
        {
            try
            {

                string fullPath = Directory.GetCurrentDirectory() + request.RawUrl;
                if (Directory.Exists(fullPath)) // если каталог
                {
                    var info = new DirectoryInfo(fullPath);
                    response.Headers.Add("Date", info.CreationTime.ToString());
                    response.Headers.Add("Name", info.Name.ToString());
                    response.Headers.Add("directory", info.Root.ToString());
                    response.Headers.Add("attribute", info.Attributes.ToString());


                }
                else if (File.Exists(fullPath)) // если файл
                {
                    FileInfo info = new FileInfo(fullPath);
                    response.Headers.Add("Date", info.CreationTime.ToString());
                    response.Headers.Add("Name", info.Name.ToString());
                    response.Headers.Add("readonly", info.IsReadOnly.ToString());
                    response.Headers.Add("length", info.Length.ToString());

                }

            }
            finally
            {
                response.OutputStream.Close();
            }


        }

        public void deleteCommand(HttpListenerRequest request, HttpListenerResponse response) // удаление
        {
            try
            {
                string name = Directory.GetCurrentDirectory() + "/";
                string fullPath = Directory.GetCurrentDirectory() + request.RawUrl; // получение полного пути
                if (fullPath == name)
                {
                    response.StatusCode = 403;
                    return;
                }
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                }
                else if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);

                }
                else
                {
                    response.StatusCode = 404;
                }

            }
            finally
            {
                response.OutputStream.Close(); 
            }

        }
    }
}
