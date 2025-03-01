using System.CommandLine;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WhisperOnline
{
    class Program
    {
        private const string ApiUrl = "https://api.openai.com/v1/audio/transcriptions";
        private const string Version = "1.0.0";
        private static bool Verbose = false;
        
        static async Task<int> Main(string[] args)
        {
            // Define command-line options to match local Whisper parameters
            var fileOption = new Option<FileInfo?>(
                name: "--file",
                description: "The audio file to transcribe")
            {
                IsRequired = true
            };

            var modelOption = new Option<string>(
                name: "--model",
                description: "Model to use for transcription",
                getDefaultValue: () => "whisper-1");

            var languageOption = new Option<string?>(
                name: "--language",
                description: "Language of the audio file");

            var outputOption = new Option<string?>(
                name: "--output",
                description: "Output file path (if not specified, uses input filename with appropriate extension)");

            var temperatureOption = new Option<float>(
                name: "--temperature",
                description: "Temperature for sampling",
                getDefaultValue: () => 0.0f);

            var promptOption = new Option<string?>(
                name: "--prompt",
                description: "Initial prompt for the transcription");

            var responseFormatOption = new Option<string>(
                name: "--response-format",
                description: "Response format (json, text, srt, verbose_json, vtt)",
                getDefaultValue: () => "text");

            var timestampsOption = new Option<bool>(
                name: "--word-timestamps",
                description: "Include word-level timestamps",
                getDefaultValue: () => false);

            var translateOption = new Option<bool>(
                name: "--translate",
                description: "Translate to English",
                getDefaultValue: () => false);

            var apiKeyOption = new Option<string?>(
                name: "--api-key",
                description: "OpenAI API key");
                
            var verboseOption = new Option<bool>(
                name: "--verbose",
                description: "Display detailed processing information",
                getDefaultValue: () => false);

            // Create root command
            var rootCommand = new RootCommand("WhisperOnline - A proxy to the OpenAI Whisper API");
            rootCommand.Description = $"WhisperOnline v{Version} - A proxy to the OpenAI Whisper API";
            
            // Add options to the command
            rootCommand.AddOption(fileOption);
            rootCommand.AddOption(modelOption);
            rootCommand.AddOption(languageOption);
            rootCommand.AddOption(outputOption);
            rootCommand.AddOption(temperatureOption);
            rootCommand.AddOption(promptOption);
            rootCommand.AddOption(responseFormatOption);
            rootCommand.AddOption(timestampsOption);
            rootCommand.AddOption(translateOption);
            rootCommand.AddOption(apiKeyOption);
            rootCommand.AddOption(verboseOption);

            // Set the handler
            rootCommand.SetHandler(async (context) =>
            {
                Verbose = context.ParseResult.GetValueForOption(verboseOption);
                
                var file = context.ParseResult.GetValueForOption(fileOption);
                var model = context.ParseResult.GetValueForOption(modelOption) ?? "whisper-1";
                var language = context.ParseResult.GetValueForOption(languageOption);
                var outputPath = context.ParseResult.GetValueForOption(outputOption);
                var temperature = context.ParseResult.GetValueForOption(temperatureOption);
                var prompt = context.ParseResult.GetValueForOption(promptOption);
                var responseFormat = context.ParseResult.GetValueForOption(responseFormatOption) ?? "text";
                var wordTimestamps = context.ParseResult.GetValueForOption(timestampsOption);
                var translate = context.ParseResult.GetValueForOption(translateOption);
                var apiKey = context.ParseResult.GetValueForOption(apiKeyOption);

                await TranscribeAudioAsync(
                    file,
                    model,
                    language,
                    outputPath,
                    temperature,
                    prompt,
                    responseFormat,
                    wordTimestamps,
                    translate,
                    apiKey);
            });

            // Parse the command line
            return await rootCommand.InvokeAsync(args);
        }

        private static async Task TranscribeAudioAsync(
            FileInfo? file,
            string model,
            string? language,
            string? outputPath,
            float temperature,
            string? prompt,
            string responseFormat,
            bool wordTimestamps,
            bool translate,
            string? apiKey)
        {
            if (file == null || !file.Exists)
            {
                Console.Error.WriteLine("Error: The specified audio file does not exist.");
                return;
            }

            // Get API key from environment variable if not provided
            string actualApiKey = apiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
            
            if (string.IsNullOrEmpty(actualApiKey))
            {
                Console.Error.WriteLine("Error: OpenAI API key is required. Provide it with --api-key or set the OPENAI_API_KEY environment variable.");
                Console.Error.WriteLine("To set the environment variable:");
                Console.Error.WriteLine("  - Windows (Command Prompt): set OPENAI_API_KEY=your_api_key");
                Console.Error.WriteLine("  - Windows (PowerShell): $env:OPENAI_API_KEY=\"your_api_key\"");
                Console.Error.WriteLine("  - Linux/macOS: export OPENAI_API_KEY=your_api_key");
                return;
            }

            if (Verbose)
            {
                Console.WriteLine($"Processing file: {file.FullName}");
                Console.WriteLine($"Model: {model}");
                if (!string.IsNullOrEmpty(language))
                    Console.WriteLine($"Language: {language}");
                if (!string.IsNullOrEmpty(outputPath))
                    Console.WriteLine($"Output path: {outputPath}");
                Console.WriteLine($"Temperature: {temperature}");
                if (!string.IsNullOrEmpty(prompt))
                    Console.WriteLine($"Prompt: {prompt}");
                Console.WriteLine($"Response format: {responseFormat}");
                Console.WriteLine($"Word timestamps: {wordTimestamps}");
                Console.WriteLine($"Translate: {translate}");
                Console.WriteLine("Sending request to OpenAI API...");
            }

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", actualApiKey);

            using var formContent = new MultipartFormDataContent();
            
            // Add file content
            using var fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);
            using var streamContent = new StreamContent(fileStream);
            formContent.Add(streamContent, "file", file.Name);
            
            // Add model
            formContent.Add(new StringContent(model), "model");
            
            // Add optional parameters if provided
            if (!string.IsNullOrEmpty(language))
            {
                formContent.Add(new StringContent(language), "language");
            }
            
            if (!string.IsNullOrEmpty(prompt))
            {
                formContent.Add(new StringContent(prompt), "prompt");
            }
            
            // Map response format
            formContent.Add(new StringContent(responseFormat), "response_format");
            
            // Add temperature
            formContent.Add(new StringContent(temperature.ToString()), "temperature");
            
            // Add timestamps option if true
            if (wordTimestamps)
            {
                formContent.Add(new StringContent("true"), "timestamp_granularities[]", "word");
            }

            try
            {
                // Make the API request
                string apiEndpoint = translate ? 
                    "https://api.openai.com/v1/audio/translations" : 
                    "https://api.openai.com/v1/audio/transcriptions";
                    
                if (Verbose)
                {
                    Console.WriteLine($"API Endpoint: {apiEndpoint}");
                }
                
                HttpResponseMessage response = await httpClient.PostAsync(apiEndpoint, formContent);
                
                // Read the response
                string responseBody = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    if (Verbose)
                    {
                        Console.WriteLine("Request successful!");
                    }
                    
                    // Determine output file path if not specified
                    string finalOutputPath = outputPath;
                    if (string.IsNullOrEmpty(finalOutputPath))
                    {
                        // Generate output file path based on input file and response format
                        string extension = GetExtensionForFormat(responseFormat);
                        finalOutputPath = Path.ChangeExtension(file.FullName, extension);
                        
                        if (Verbose)
                        {
                            Console.WriteLine($"No output path specified. Using: {finalOutputPath}");
                        }
                    }
                    
                    // Process the response based on the requested output format
                    ProcessResponse(responseBody, responseFormat, finalOutputPath);
                }
                else
                {
                    // Handle error
                    Console.Error.WriteLine($"Error: {response.StatusCode}");
                    Console.Error.WriteLine(responseBody);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                if (Verbose)
                {
                    Console.Error.WriteLine(ex.StackTrace);
                }
            }
        }
        
        private static string GetExtensionForFormat(string format)
        {
            return format.ToLower() switch
            {
                "json" => ".json",
                "verbose_json" => ".json",
                "srt" => ".srt",
                "vtt" => ".vtt",
                _ => ".txt"
            };
        }

        private static void ProcessResponse(string responseBody, string responseFormat, string? outputPath)
        {
            try
            {
                if (Verbose)
                {
                    Console.WriteLine($"Processing response in {responseFormat} format...");
                }
                
                string processedContent;
                
                switch (responseFormat.ToLower())
                {
                    case "json":
                    case "verbose_json":
                        // Pretty print JSON
                        var jsonObj = JObject.Parse(responseBody);
                        processedContent = jsonObj.ToString(Formatting.Indented);
                        break;
                    
                    case "text":
                        // If it's JSON, extract the text field
                        if (responseBody.TrimStart().StartsWith("{"))
                        {
                            var jsonResponse = JObject.Parse(responseBody);
                            processedContent = jsonResponse["text"]?.ToString() ?? responseBody;
                        }
                        else
                        {
                            // Otherwise, use as is
                            processedContent = responseBody;
                        }
                        break;
                    
                    case "srt":
                    case "vtt":
                        // For subtitle formats, use as is
                        processedContent = responseBody;
                        break;
                    
                    default:
                        // Default to raw output
                        processedContent = responseBody;
                        break;
                }
                
                // If output path is specified, write to file
                if (!string.IsNullOrEmpty(outputPath))
                {
                    File.WriteAllText(outputPath, processedContent);
                    Console.WriteLine($"Output written to: {outputPath}");
                }
                else
                {
                    // Otherwise, write to console
                    Console.WriteLine(processedContent);
                }
            }
            catch (Exception ex)
            {
                // If parsing fails, output raw response
                Console.Error.WriteLine($"Warning: Could not process response in {responseFormat} format. Outputting raw response.");
                if (Verbose)
                {
                    Console.Error.WriteLine($"Error details: {ex.Message}");
                }
                
                if (!string.IsNullOrEmpty(outputPath))
                {
                    File.WriteAllText(outputPath, responseBody);
                    Console.WriteLine($"Raw output written to: {outputPath}");
                }
                else
                {
                    Console.WriteLine(responseBody);
                }
            }
        }
    }
}
