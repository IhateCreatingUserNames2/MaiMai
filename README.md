# README for AI Agent Interaction System

## Overview
This AI Agent Interaction system is built using Unity and integrates with LLM (Large Language Model) capabilities for interactive dialogue and agent management. The system allows creating, interacting with, and managing multiple AI agents, each with a personality, background, and memory capabilities. It is designed for use in multiplayer environments and supports interactions via Photon Chat.

## File Descriptions

### 1. `AIAgentInteraction.cs`
This script handles interactions between the player and an assigned AI agent. It manages assigning agents to the interaction system, tracking player proximity, and invoking conversations via dialogue UI elements.

**Key Features:**
- Assigns an AI agent for interaction.
- Tracks player entry and exit from interaction zones via Unity triggers.
- Opens dialogue input UI for player interactions and processes responses.
- Integrates with `PicoDialogue` for managing the dialogue flow.
- Logs and manages interactions, ensuring the agent's context is set up properly.

### 2. `AIManager.cs`
The core manager for handling all AI agents within the game. It ensures that agents are loaded, registered, and properly managed throughout the game.

**Key Features:**
- Registers new AI agents.
- Provides functions to retrieve AI agents by name.
- Loads agents and their chat history on game start.
- Manages all active AI agents globally, ensuring persistence between scenes.

### 3. `AIAgentManager.cs`
This script manages the UI for selecting and spawning AI agents in the game. It controls dropdown options for selecting agents and handles the spawning and despawning of AI agents in the scene.

**Key Features:**
- Populates dropdown UI with available agents.
- Handles the logic for spawning and despawning AI agents.
- Saves the agent's chat data and history using `LLMCharacterMemoryManager`.

### 4. `PicoDialogue.cs`
Manages the dialogue system that allows players to interact with AI agents through text input. It includes the logic for sending player messages to the AI agent and displaying AI responses via UI elements.

**Key Features:**
- Handles dialogue input UI for player interactions.
- Manages the connection to Photon Chat for multiplayer interactions.
- Supports voice integration (on Android and iOS) using Text-to-Speech (TTS).
- Retrieves and displays chat history during interactions.

### 5. `PermissionManager.cs`
This script manages permissions for Android devices, ensuring that necessary permissions (e.g., storage, microphone, location) are requested when the game starts.

**Key Features:**
- Requests storage, microphone, camera, and location permissions for Android.
- Provides utility to check if all permissions have been granted.

### 6. `ModelSelector.cs`
This script allows players to select and change the LLM model being used by the AI agents through a dropdown UI.

**Key Features:**
- Populates a dropdown with available AI models from a JSON file (`models.json`).
- Handles model switching by destroying the current LLM instance and loading the new model.

### 7. `AIAgent.cs`
Represents individual AI agents in the game. Each agent has an ID, name, personality, background, and their own conversation history.

**Key Features:**
- Stores agent-specific information like personality and background.
- Maintains a history of conversations with players.
- Uses `LLMCharacter` for managing interactions and conversation generation.
- Embeds and retrieves relevant context for interactions using memory capabilities.

### 8. `AgentCreateUI.cs`
This script handles the UI for creating new AI agents. Players can define a new agent’s name, personality, and background, which is then registered and made available in the dropdown list.

**Key Features:**
- UI input for creating agents with custom names, personalities, and backgrounds.
- Registers the newly created agent in the system for future interactions.

### 9. `SaveSystem.cs`
A utility class for saving and loading AI agents' data. It manages saving agent configurations and their conversation history to disk.

**Key Features:**
- Saves and loads all AI agents and their associated data (including conversation history).
- Ensures agent data is persistent across sessions using JSON serialization.

### 10. `LLMCharacterMemoryManager.cs`
This script is responsible for managing AI agents' memory and chat history. It uses embeddings to store and retrieve relevant parts of past conversations.

**Key Features:**
- Embeds and saves chat history for each AI agent.
- Retrieves relevant context based on player input for use in conversation generation.
- Saves and loads embeddings to persist the memory across sessions.

## Usage Instructions

### Setting up AI Agents:
- Use the `AgentCreateUI` script to create new AI agents by defining their name, personality, and background.
- Agents will be registered in the `AIManager`, and their data will be saved for future sessions.

### Interacting with AI Agents:
- When the player enters an AI agent's interaction zone, the `AIAgentInteraction` script will trigger the dialogue system.
- Players can input text through the dialogue UI to interact with the agent.
- The AI agent will generate a response using the LLM and context retrieved from its memory.

### Switching AI Models:
- Use the `ModelSelector` to switch between different LLM models by selecting a model from the dropdown menu and applying the change.

### Managing Permissions (Android):
- The `PermissionManager` script will automatically request necessary permissions when the game starts on Android devices. Ensure all required permissions are granted for the full functionality of the game.

### Saving and Loading Data:
- All AI agents’ data and chat history will be saved to persistent storage by the `SaveSystem`. This ensures that agents and their conversation history are available across different gaming sessions.
