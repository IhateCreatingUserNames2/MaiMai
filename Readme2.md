# AI Agent Interaction System

This project demonstrates an AI-driven NPC interaction system using a combination of AI models, memory management, and agent behavior logic for Unity. The NPCs can interact with the player using large language models (LLM) and manage their own memories for more personalized and context-aware responses.

## Table of Contents

- [Components Overview](#components-overview)
- [Setup Instructions](#setup-instructions)
- [Prefabs and GameObjects](#prefabs-and-gameobjects)
- [How to Use](#how-to-use)
- [System Flow](#system-flow)

---

## Components Overview

1. **AgentCreateUI**
   - Manages the UI for creating new AI agents, allowing users to specify agent name, personality, and background, and register them in the system.
   - Handles the creation and saving of agent data.

2. **PicoDialogue**
   - Core system for NPC-player dialogue, using an LLM to generate responses and manage NPC dialogue UI. It integrates with Photon for multiplayer chat handling.

3. **LLMCharacterMemoryManager**
   - Manages agent memory using embeddings, ensuring relevant information is retained for NPCs across interactions. This includes chat history and context retrieval.

4. **AIAgentManager**
   - Oversees the registration, spawning, and management of AI agents in the scene.
   - Handles agent-related UI elements like agent selection dropdowns and buttons for spawning/despawning agents.

5. **AIManager**
   - Centralized agent management that keeps track of all active and registered AI agents.

6. **ModelSelector**
   - Provides UI for selecting different AI models for agents. This allows changing the model at runtime based on user input.

7. **AIAgentInteraction**
   - Controls player interactions with NPCs, handling proximity-based triggers to initiate dialogue. It ensures smooth data exchange between the AI agent and the dialogue system.

---

## Setup Instructions

1. Download Files, put inside your Project
2. Create GameObjects and Link the Components
3. configure fields in inspector , check all GameObjects. 
4. add AiAgentInteraction.cs component to NPC PreFab
5. Create UI
6. Create new AI Agent in Game and Spawn it

---

## Prefabs and GameObjects

1. **AgentCreateUI**
   - Handles the UI for creating and configuring new agents.
   - Components:
     - `TMP_InputField`: For agent name, personality, and background input.
     - `Button`: For creating and registering new agents.

2. **PicoDialogue and LLMCharacter** (Same GameObject)
   - Controls AI interaction and manages the display of dialogues in the game world.

3. **MemoryManager** (LLMCharacterMemoryManager & Embeddings)
   - Handles memory embeddings and search functionalities to make the agents context-aware.

4. **LLM**
   - The LLM model used by agents for generating responses.

5. **ModelSelect**
   - Provides the UI for selecting different LLM models.

6. **AiAgentManager**
   - Manages the spawning of agents and handles the UI interactions for selecting and despawning agents.

---

## How to Use

1. **Agent Creation**:
   - In the main menu or a specified UI, use the `AgentCreateUI` to input the name, personality, and background of your AI agents.
   - Click "Create Agent" to register them in the system. The agent will be available for interaction in the game.

2. **Model Selection**:
   - Use the `ModelSelector` UI to change the LLM model used by the NPC. This allows dynamic switching of models in runtime based on the scenario or player preferences.

3. **Interaction**:
   - When the player approaches an NPC with the `AiAgentInteraction` component, the dialogue UI will appear, allowing the player to input messages and receive AI-generated responses.
   - Agents use context from previous interactions to provide relevant and personalized responses, managed by `LLMCharacterMemoryManager`.

4. **Saving and Loading**:
   - Agents' data, including personality, background, and chat history, are saved using the `SaveSystem`. NPCs will retain memory across game sessions.

---

## System Flow

1. **Memory Management**:
   - NPCs use the `LLMCharacterMemoryManager` to embed conversation data and retrieve relevant context for future interactions. This allows agents to "remember" past conversations and build upon them.

2. **Agent Spawning**:
   - The `AiAgentManager` is responsible for dynamically spawning agents in the scene. It ensures that AI agents are properly linked to their memory and dialogue components.

3. **Model Switching**:
   - The `ModelSelector` allows dynamic switching between different LLM models, enabling flexibility in the NPC's capabilities during gameplay.

4. **NPC Interaction**:
   - Players can engage with NPCs by entering interaction zones. The `AIAgentInteraction` component ensures smooth transitions between NPCs and the dialogue system, updating the context and generating responses in real-time.

---

For further development, ensure that the correct GameObjects are assigned in the Unity inspector and that dependencies like LLMUnity and Photon are correctly installed and configured.

