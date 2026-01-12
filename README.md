<div align="center">

# Museum Quiz VR

<img src="https://img.shields.io/badge/Unity-2022.3%2B-black?logo=unity" alt="Unity Version"/>
<img src="https://img.shields.io/badge/Platform-Meta%20Quest-blue?logo=meta" alt="Platform"/>
<img src="https://img.shields.io/badge/AI-Google%20Gemini-orange?logo=google" alt="AI"/>
<img src="https://img.shields.io/badge/License-MIT-green" alt="License"/>

**An educational VR quiz game exploring world monuments through AI-generated questions**

[Features](#features) • [Installation](#installation) • [Architecture](#architecture) • [Team](#team)

</div>

---

## Overview

Museum Quiz VR is an immersive virtual reality educational game for Meta Quest headsets. Players explore a virtual museum containing 31 paintings of famous world monuments. By interacting with each painting, an AI-powered quiz question is dynamically generated using the Google Gemini API.

### Game Objectives

- Reach a target score (configurable, default: 500 points)
- Complete a set number of paintings (configurable, default: 5)
- Beat the clock (configurable, default: 5 minutes)

## Features

- **Immersive VR Experience** - Full 6DOF support for Meta Quest headsets
- **31 World Monuments** - From the Eiffel Tower to the Pyramids of Giza
- **AI-Generated Questions** - Dynamic quiz generation via Google Gemini API
- **Futuristic UI** - Holographic cyan/blue interface with glow effects
- **Configurable Gameplay** - Adjustable score targets, painting goals, and time limits
- **Wrist HUD** - Smartwatch-style display showing timer and score

## Screenshots

> *Screenshots coming soon*

## Installation

### Prerequisites

- Unity 2022.3 LTS or later
- Meta Quest SDK / Oculus Integration
- Google Gemini API key

### Setup

1. Clone the repository
   ```bash
   git clone https://github.com/your-username/AI-VR-Learning.git
   ```

2. Open the project in Unity

3. Configure the API key
   - Create an `ApiConfig` asset: `Create > Museum AI > Api Config`
   - Enter your Google Gemini API key

4. Build and deploy to Meta Quest

## Architecture

```
Assets/_Game/
├── Scripts/
│   ├── API/           # Gemini API integration
│   ├── Core/          # Game state management
│   ├── Gameplay/      # Player interaction & paintings
│   └── UI/            # Quiz, HUD, menus, futuristic styles
├── Prefabs/           # UI panels and reusable objects
└── Paintings/         # Monument images and materials
```

### Key Components

| Component | Description |
|-----------|-------------|
| `GameManager` | Singleton managing game state, score, timer, and win conditions |
| `APIManager` | Handles Google Gemini API calls for quiz generation |
| `PaintingController` | Attached to each painting with monument metadata |
| `PlayerInteraction` | VR raycast interaction system |
| `QuizUIController` | Quiz panel display and answer handling |

## Monuments

The museum features 31 famous monuments including:

- Tour Eiffel (Paris)
- Colosseum (Rome)
- Sagrada Familia (Barcelona)
- Pyramids of Giza (Egypt)
- Machu Picchu (Peru)
- Great Wall of China
- *...and 25 more*

## Configuration

Game settings can be adjusted in the Main Menu:

| Setting | Range | Default |
|---------|-------|---------|
| Target Score | 100 - 1000 | 500 |
| Paintings to Complete | 1 - 10 | 5 |
| Time Limit | 1 - 10 min | 5 min |

## Tech Stack

- **Engine**: Unity 2022.3 LTS
- **VR SDK**: Meta XR SDK / Oculus Integration
- **AI API**: Google Gemini 2.0 Flash
- **Language**: C#

## Team

**Equipe 7 ISEP**

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Meta for the Quest SDK
- Google for the Gemini API
- ISEP for the educational framework

---

<div align="center">

Made with Unity and Meta Quest

</div>
