# PixelLab

PixelLab is a powerful image processing and color analysis desktop application built with .NET 10 and Windows Forms. It provides advanced tools for exploring color spaces, manipulating images, and inspecting pixel data using a modern dark-themed UI.

## Features

- **Advanced Color Processing**: Transform and manipulate colors using a highly optimized engine.
- **Color Space Exploration**: Visualize and analyze images in various color spaces.
- **Pixel Inspection**: Detailed inspection of individual pixels within an image workspace.
- **Color Quantization**: Reduce image palettes efficiently using built-in quantization algorithms.
- **Modern UI**: A customized dark theme interface featuring custom controls like `ColorSlider`, `DarkButton`, and `StatCard`.
- **High Performance**: Utilizes `LockBitmap` and unsafe blocks for fast, memory-efficient image processing.

## Technologies Used

- **.NET 10**: Targeting the latest `net10.0-windows` framework for optimal performance and access to the newest C# 14 features.
- **Windows Forms**: Classic desktop UI development enhanced with custom drawing and modern aesthetics.
- **Emgu.CV (v4.12)**: Powerful computer vision and image processing capabilities.
- **MathNet.Numerics (v5.0)**: Advanced mathematical computations.
- **NAudio (v2.3)**: Audio playback and processing (if applicable to future multimedia features).

## Architecture

The application is structured into clearly defined modules:

- **Core**: Contains fundamental components like `ImageWorkspace`.
- **ColorProcessing**: The brain behind color manipulation (`ColorEngine`, `ColorMath`, `ColorSpaces`, `ColorSpaceRenderer`, and the fast `LockBitmap`).
- **UI**: 
  - **Forms**: Main application windows (`MainForm`, `ColorSpaceViewerForm`).
  - **Controls**: Reusable UI elements tailored for a dark theme (`ColorSlider`, `DarkButton`, `PixelInspectorPanel`, `StatCard`).
- **Utils**: Helper classes and extension methods (`GraphicsExtensions`, `ImageHelper`).

## Getting Started

### Prerequisites

- Visual Studio 2026 (or later) with the .NET desktop development workload.
- .NET 10 SDK.

### Building and Running

1. Clone the repository: `git clone https://github.com/mouaz-ajaj/PixelLab`
2. Open `PixelLab.sln` or `PixelLab/PixelLab.csproj` in Visual Studio.
3. Ensure the target platform is set appropriately (x64 is supported).
4. Build and run the project (F5).

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is open-source and available under the MIT License.
