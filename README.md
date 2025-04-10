# MaiMai AI Agent System


![small222](https://github.com/user-attachments/assets/a0062876-25a0-40fe-bfe1-ce24281808cb)


## Overview

MaiMai is an AI Agent System for Unity that allows developers to create and manage intelligent NPCs powered by large language models. Built on top of LLMUnity, MaiMai enables:

- **Dynamic AI Agent Creation**: Spawn AI agents with custom names and system prompts at runtime
- **Fixed AI Agents**: Set up predefined NPC agents directly in your scenes
- **Memory Management**: Each AI agent maintains its own conversation history
- **Retrieval-Augmented Generation (RAG)**: Enhance AI responses with contextual knowledge
- **Multi-Agent Support**: Create and manage multiple unique AI characters

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [System Architecture](#system-architecture)
- [Components](#components)
- [Usage Examples](#usage-examples)
- [Configuration](#configuration)
- [Demo Scene](#demo-scene)
- [Support](#support)
- [License](#license)

## Features

- **Runtime Agent Creation**: Create AI agents with custom names and personalities during gameplay
- **Persistent Memory**: Save and load conversation history for continuity across sessions
- **Context-Aware AI**: Agents can retrieve relevant information from their memory to enhance responses
- **Proximity & Key-Triggered Interactions**: Configure how players interact with AI agents
- **Fixed Memory Support**: Pre-load knowledge directly into agents from text files
- **Mobile Support**: Works on Android ( Idk if Works on iOS) 
- **User-Friendly UI**: Simple interface for creating and interacting with AI agents

## Installation

### Prerequisites

- Unity 2021 LTS or newer
- LLMUnity package
- Newtonsoft JSON package

### Installation Steps

1. **Install LLMUnity**:
   - Via GitHub: Add package from git URL `https://github.com/undreamai/LLMUnity.git`
   - Or via Asset Store: [LLMUnity Asset](https://assetstore.unity.com/packages/slug/273604)

2. **Install Newtonsoft JSON**:
   - Open Package Manager in Unity
   - Click "Add package by name"
   - Enter: `com.unity.nuget.newtonsoft-json`
   - Click "Add"

3. **Install MaiMai**:
   
   **Option 1: Full Demo Package**
   - Download the [Demo-Ready Package](https://drive.google.com/file/d/1oo_H8AYmFuU8LzrqVf4MdjScLZRubJIe/view?usp=sharing)
   - Import the package into your Unity project
   
   **Option 2: Code-Only Package**
   - Download the [Code-Only Package](https://github.com/IhateCreatingUserNames2/MaiMai/blob/main/MaiMaiNoLang.1.4.Small.unitypackage)
   - Import the package into your Unity project

## Quick Start

1. **Configure LLM**:
   - Create a GameObject and add the `LLM` component
   - Download or load a model using the LLM Model Manager

2. **Configure RAG**:
   - Create a GameObject and add the `RAG` component
   - Select your preferred search and chunking methods

3. **Setup UI**:
   - Configure the required UI components (see Configuration section)

4. **Run Your Scene**:
   - Start your game
   - Create AI agents or interact with fixed agents

## System Architecture

MaiMai is built around several key components that work together:

- **AIManager**: Singleton that tracks and manages all AI agents in the game
- **AIAgent**: Core class representing an AI character with its own memory and personality
- **AIAgentInteraction**: Handles player proximity and interaction triggers
- **AIAgentManager**: Manages spawning and despawning of AI agents
- **AgentCreateUI**: UI for creating new AI agents at runtime
- **PicoDialogue**: Manages dialogue UI and interaction flow
- **LLMCharacterMemoryManager**: Handles semantic memory storage and retrieval

## Components

![image](https://github.com/user-attachments/assets/3570ec74-2062-4828-bed0-26bc4f1f8b61)



### AIManager

The central registry for all AI agents. It:
- Maintains a dictionary of all active AI agents
- Provides methods to retrieve agents by name or ID
- Handles loading agents from saved data on startup

### AIAgent

Represents an individual AI character. Each agent has:
- Unique ID and name
- Custom system prompt defining personality
- Conversation history per user
- Memory management system
- Methods for interacting with users

### AIAgentInteraction

Controls how players can interact with AI agents in the scene:
- Proximity detection via colliders
- Key-triggered interactions
- Fixed memory loading for pre-defined knowledge

### AgentCreateUI

UI component for creating new AI agents with:
- Custom name fields
- Custom system prompt fields
- Creation button

### PicoDialogue

Manages the dialogue UI system:
- Displays AI responses
- Handles player input
- Optional text-to-speech functionality

### LLMCharacterMemoryManager

Manages the semantic memory system:
- Embeds messages into vector space
- Retrieves contextually relevant information
- Supports both dynamic and fixed memory

## Usage Examples

### Creating a Fixed AI Agent in a Scene

```csharp
// Add AIAgentInteraction component to your NPC GameObject
AIAgentInteraction interaction = npcObject.AddComponent<AIAgentInteraction>();

// Configure the agent
interaction.isFixedAgent = true;
interaction.fixedAgentName = "Shop Keeper";
interaction.fixedAgentPrompt = "You are a friendly shop keeper in a fantasy town. You sell potions and magical items.";

// Optionally add fixed memory
interaction.hasFixedMemory = true;
interaction.memoryFiles = new List<TextAsset>() { shopInventoryText };
```

### Creating an AI Agent at Runtime

```csharp
// Get references
AIAgentManager aiManager = FindObjectOfType<AIAgentManager>();
string agentName = "Personal Assistant";
string customPrompt = "You are a helpful AI assistant that accompanies the player.";

// Spawn the agent
aiManager.SpawnAI(agentName);
```

### Interacting with an AI Agent

```csharp
AIAgentInteraction interaction = FindObjectOfType<AIAgentInteraction>();
string userId = "Player1";
string message = "Hello, who are you?";

// Send message to agent
await interaction.Interact(userId, message, (response) => {
    Debug.Log("AI responded: " + response);
});
```

## Configuration

### Required UI Setup

Configure the following UI components:

1. **AI Agent Manager:**
   - Select AI Button → `AIAgentManager.OnSelectAIClicked`
   - Despawn Button → `AIAgentManager.DespawnAllAgents`
   - AI Dropdown → Assign your dropdown component

2. **Agent Create UI:**
   - Create Agent Button → `AgentCreateUI.OnCreateAgentClicked`
   - Agent Name Input → Assign your input field
   - Custom Prompt Input → Assign your input field

3. **Dialogue UI:**
   - Send Button → `PicoDialogue.OnSendButtonClicked`
   - Player Input Field → Assign your input field
   - Dialogue Canvas → Assign your canvas
   - Toggle Panel Button → `ToggleAIUIButton.ToggleAIUIButtonVisibility`

### LLM Configuration

Configure the LLM component:

1. Download or load a model using the Model Manager
2. Adjust inference parameters (temperature, context size, etc.)
3. Configure GPU acceleration if needed

### RAG Configuration

Configure the RAG component:

1. Select search method (DBSearch recommended)
2. Select chunking method (SentenceSplitter recommended)
3. Download or load an embedding model

## Demo Scene

The demo scene showcases MaiMai in a night city environment:

- Location: `MaiMai/Scene/demo night city`
- Features demonstrated:
  - AI agent creation interface
  - Fixed AI NPCs
  - Dynamic AI spawning
  - Dialogue interaction system

### Required Assets for Demo

The full demo requires:
- [Invector Third Person Controller (Free)](https://assetstore.unity.com/packages/tools/game-toolkits/third-person-controller-basic-locomotion-free-82048)
- [Demo City by Versatile Studio](https://assetstore.unity.com/packages/3d/environments/urban/demo-city-by-versatile-studio-mobile-friendly-269772)
- [Picochan 3D Character](https://assetstore.unity.com/packages/3d/characters/humanoids/picochan-220038)

## Support

For help, questions, or feature requests:
- Join LLmUnity Discord  [Discord Server](https://discord.gg/VYsaEZJb)
- Submit issues on our [GitHub repository](https://github.com/IhateCreatingUserNames2/MaiMai)

## License

MaiMai is released under the MIT License. 

LLMUnity is also released under the MIT License. Some LLM models may have their own licensing terms - please review them before use.
