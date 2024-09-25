# LiteQuark

**LiteQuark** is a lightweight Unity framework designed to streamline development workflows and enhance productivity for game developers. This framework provides core utilities, components, and modular systems, making it easier to manage complex game logic and structure within Unity projects.

## Features

- **Modular Architecture**: A simple and extendable system for structuring your Unity project.
- **Utility Components**: Ready-to-use components to speed up common game development tasks.
- **Customizable Systems**: Pre-built systems that are easy to customize to meet the unique needs of your game.
- **Lightweight**: Minimal performance overhead, designed to be used with small to mid-sized games.
  
## Getting Started

### Prerequisites

To get started with LiteQuark, you need:

- Unity version 2020.3 or higher.
- Basic knowledge of Unity and C# programming.

### Installation

1. Clone this repository into your Unity project’s `Assets` folder.

   ```bash
   git clone https://github.com/UnSkyToo/LiteQuark.git
   ```

2. Import the cloned assets into your Unity project.
   
3. After importing, you will find the LiteQuark folder within your project’s Assets directory.

### Usage

Once the framework is added to your project:

1. Browse the available utilities and components in the **LiteQuark** folder.
2. You can integrate pre-built systems into your game or customize them to suit your needs.
3. Extend the framework by adding your own components and scripts, following the modular structure of LiteQuark.

### Example

Here’s a simple example of how to use one of the included systems:

``` bash
1. Create Empy GameObject and attatch LiteLauncher
2. Create GameLogic.cs and implement ILogic interface
3. add your logic in LiteLauncher Inspector
4. use LiteRuntime to access all modules
5. create StandaloneAssets folder, it's main assets folder
```

```csharp
using System;
using LiteQuark.Runtime;

public class GameLogic : ILogic
{
    public bool Startup()
    {
        LiteRuntime.Audio.PlayMusic("xxxx.ogg", true, 0.5f); // play music with Audio Module
        return true;
    }

    public void Shutdown()
    {
    }

    public void Tick(float deltaTime)
    {
    }
}
```

## Documentation

Full documentation for LiteQuark is still in progress. In the meantime, feel free to explore the source code and check out the comments for more insights into the framework’s structure and usage.

## Contributing

Contributions to LiteQuark are welcome! If you want to improve the framework, feel free to:

1. Fork the repository.
2. Create a new branch for your feature or bugfix.
3. Submit a pull request once your changes are ready.

### Development Guidelines

- Keep the code modular and maintainable.
- Ensure that your changes do not negatively impact performance.
- Write clean, readable, and well-documented code.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contact

For questions, suggestions, or feedback, feel free to open an issue in this repository or contact me at [unskytoo@gmail.com](mailto:unskytoo@gmail.com).
