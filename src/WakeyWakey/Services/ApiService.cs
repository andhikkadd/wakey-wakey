using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using WakeyWakey.Models;

namespace WakeyWakey.Services
{
    public class ApiService
    {
        private HttpListener? _listener;
        private bool _isRunning;
        private const string Url = "http://localhost:18790/";

        public void Start()
        {
            if (_isRunning) return;

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(Url);
                _listener.Start();
                _isRunning = true;
                Task.Run(() => ListenLoop());
                System.Diagnostics.Debug.WriteLine($"API Server started on {Url}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start API Server: {ex.Message}");
            }
        }

        public void Stop()
        {
            _isRunning = false;
            try
            {
                _listener?.Stop();
                _listener?.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping API Server: {ex.Message}");
            }
        }

        private async Task ListenLoop()
        {
            while (_isRunning && _listener != null && _listener.IsListening)
            {
                try
                {
                    HttpListenerContext context = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequest(context)); // Handle concurrently
                }
                catch (HttpListenerException)
                {
                    // Listener stopped
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in ListenLoop: {ex.Message}");
                }
            }
        }

        private async Task HandleRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            // CORS headers
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, DELETE, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = (int)HttpStatusCode.OK;
                response.Close();
                return;
            }

            string path = request.Url?.AbsolutePath.ToLower() ?? "";
            try
            {
                if (path == "/alarms")
                {
                    if (request.HttpMethod == "GET")
                    {
                        var alarms = AlarmStorage.LoadAlarms();
                        string json = JsonSerializer.Serialize(alarms);
                        await SendJsonResponse(response, json, HttpStatusCode.OK);
                    }
                    else if (request.HttpMethod == "POST")
                    {
                        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8))
                        {
                            string body = await reader.ReadToEndAsync();
                            var incomingAlarm = JsonSerializer.Deserialize<Alarm>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (incomingAlarm == null)
                            {
                                await SendJsonResponse(response, "{\"error\": \"Invalid alarm data\"}", HttpStatusCode.BadRequest);
                                return;
                            }

                            var alarms = AlarmStorage.LoadAlarms();
                            var existingAlarm = alarms.FirstOrDefault(a => a.Id == incomingAlarm.Id);

                            if (existingAlarm != null)
                            {
                                // Update existing alarm properties
                                existingAlarm.Hour = incomingAlarm.Hour;
                                existingAlarm.Minute = incomingAlarm.Minute;
                                existingAlarm.Label = incomingAlarm.Label ?? existingAlarm.Label;
                                existingAlarm.RepeatDays = incomingAlarm.RepeatDays;
                                existingAlarm.IsEnabled = incomingAlarm.IsEnabled;
                                existingAlarm.Difficulty = incomingAlarm.Difficulty;
                                existingAlarm.ChallengeRequiredStreak = incomingAlarm.ChallengeRequiredStreak;
                                incomingAlarm = existingAlarm;
                            }
                            else
                            {
                                // Add new alarm
                                alarms.Add(incomingAlarm);
                            }

                            AlarmStorage.SaveAlarms(alarms);

                            // Sync with Task Scheduler
                            if (incomingAlarm.IsEnabled)
                            {
                                SchedulerService.RegisterAlarm(incomingAlarm);
                            }
                            else
                            {
                                SchedulerService.UnregisterAlarm(incomingAlarm.Id);
                            }

                            // Refresh UI
                            RefreshUi();

                            string json = JsonSerializer.Serialize(incomingAlarm);
                            await SendJsonResponse(response, json, HttpStatusCode.OK);
                        }
                    }
                    else if (request.HttpMethod == "DELETE")
                    {
                        string? idParam = request.QueryString["id"];
                        if (Guid.TryParse(idParam, out Guid id))
                        {
                            var alarms = AlarmStorage.LoadAlarms();
                            var alarmToRemove = alarms.FirstOrDefault(a => a.Id == id);
                            if (alarmToRemove != null)
                            {
                                alarms.Remove(alarmToRemove);
                                AlarmStorage.SaveAlarms(alarms);
                                SchedulerService.UnregisterAlarm(id);
                                RefreshUi();
                                await SendJsonResponse(response, "{\"status\": \"deleted\"}", HttpStatusCode.OK);
                            }
                            else
                            {
                                await SendJsonResponse(response, "{\"error\": \"Alarm not found\"}", HttpStatusCode.NotFound);
                            }
                        }
                        else
                        {
                            await SendJsonResponse(response, "{\"error\": \"Invalid or missing 'id' query parameter\"}", HttpStatusCode.BadRequest);
                        }
                    }
                    else
                    {
                        response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        response.Close();
                    }
                }
                else if (path == "/alarms/toggle" && request.HttpMethod == "POST")
                {
                    string? idParam = request.QueryString["id"];
                    string? enabledParam = request.QueryString["enabled"];

                    if (Guid.TryParse(idParam, out Guid id) && bool.TryParse(enabledParam, out bool isEnabled))
                    {
                        var alarms = AlarmStorage.LoadAlarms();
                        var alarm = alarms.FirstOrDefault(a => a.Id == id);
                        if (alarm != null)
                        {
                            alarm.IsEnabled = isEnabled;
                            AlarmStorage.SaveAlarms(alarms);

                            if (isEnabled)
                            {
                                SchedulerService.RegisterAlarm(alarm);
                            }
                            else
                            {
                                SchedulerService.UnregisterAlarm(id);
                            }

                            RefreshUi();
                            string json = JsonSerializer.Serialize(alarm);
                            await SendJsonResponse(response, json, HttpStatusCode.OK);
                        }
                        else
                        {
                            await SendJsonResponse(response, "{\"error\": \"Alarm not found\"}", HttpStatusCode.NotFound);
                        }
                    }
                    else
                    {
                        await SendJsonResponse(response, "{\"error\": \"Invalid or missing parameters\"}", HttpStatusCode.BadRequest);
                    }
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Request processing error: {ex.Message}");
                await SendJsonResponse(response, $"{{\"error\": \"Internal server error: {ex.Message}\"}}", HttpStatusCode.InternalServerError);
            }
        }

        private async Task SendJsonResponse(HttpListenerResponse response, string json, HttpStatusCode statusCode)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            response.StatusCode = (int)statusCode;
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            try
            {
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            finally
            {
                response.Close();
            }
        }

        private void RefreshUi()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                mainWindow?.LoadAndDisplayAlarms();
            });
        }
    }
}
