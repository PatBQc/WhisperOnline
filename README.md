# WhisperOnline

WhisperOnline is a command-line tool that acts as a proxy to the OpenAI Whisper API. It allows you to transcribe audio files using the same parameters as the local Whisper implementation, but leverages the online API for processing.

## Features

- Transcribe audio files using the OpenAI Whisper API
- Support for various output formats (text, JSON, SRT, VTT)
- Language specification
- Temperature control for sampling
- Initial prompt support
- Word-level timestamps
- Translation to English
- Verbose mode for detailed processing information

## Prerequisites

- .NET 8.0 or higher
- An OpenAI API key

## Installation

1. Clone the repository
2. Build the project:
   ```
   dotnet build
   ```

## Usage

```
WhisperOnline [options]
```

### Options

- `--file <file>` (REQUIRED): The audio file to transcribe
- `--model <model>`: Model to use for transcription (default: whisper-1)
- `--language <language>`: Language of the audio file
- `--output <output>`: Output file path (if not specified, uses input filename with appropriate extension)
- `--temperature <temperature>`: Temperature for sampling (default: 0)
- `--prompt <prompt>`: Initial prompt for the transcription
- `--response-format <response-format>`: Response format (json, text, srt, verbose_json, vtt) (default: text)
- `--word-timestamps`: Include word-level timestamps (default: False)
- `--translate`: Translate to English (default: False)
- `--api-key <api-key>`: OpenAI API key
- `--verbose`: Display detailed processing information (default: False)
- `--version`: Show version information
- `-?, -h, --help`: Show help and usage information

### API Key

You can provide your OpenAI API key in one of two ways:

1. Using the `--api-key` command-line option:
   ```
   WhisperOnline --file audio.mp3 --api-key your_api_key
   ```

2. Setting the `OPENAI_API_KEY` environment variable:
   - Windows (Command Prompt): `set OPENAI_API_KEY=your_api_key`
   - Windows (PowerShell): `$env:OPENAI_API_KEY="your_api_key"`
   - Linux/macOS: `export OPENAI_API_KEY=your_api_key`

## Examples

### Basic Transcription

```
WhisperOnline --file audio.mp3
```

### Specify Language

```
WhisperOnline --file audio.mp3 --language en
```

### Output as JSON

```
WhisperOnline --file audio.mp3 --response-format json
```

### Save to a Specific File

```
WhisperOnline --file audio.mp3 --output transcript.txt
```

### Translate to English

```
WhisperOnline --file audio.mp3 --translate
```

### Include Word-Level Timestamps

```
WhisperOnline --file audio.mp3 --word-timestamps
```

### Verbose Mode

```
WhisperOnline --file audio.mp3 --verbose
```

## License

This project is licensed under the MIT License.
