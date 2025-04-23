# MaiMai: Modular AI Agent System for Unity

MaiMai is a modular AI agent system for Unity games built on top of LLMUnity. It allows developers to create interactive NPCs powered by large language models, with memory, personality, and contextual awareness.

## 📋 Features

- **Modular Architecture**: Cleanly separated components that can be extended or replaced
- **Persistent Agents**: Create and manage AI agents that persist across game sessions
- **Memory Management**: Retrieval-Augmented Generation (RAG) for contextual memory
- **Conversation History**: Track and save chat history for consistent interactions
- **Context-Aware Responses**: Generate responses based on current and past interactions
- **Fixed Memory**: Load background knowledge from text files
- **UI Integration**: Ready-to-use dialogue system

## 🔧 Architecture

MaiMai follows a modular design with clearly defined interfaces:

### Core Module: `MaiMai.Core`

Contains all interfaces that define the system:

- `IAIAgent`: Represents an intelligent agent with personality and memory
- `IMemoryProvider`: Manages retrieval and storage of conversation memory
- `ILLMProvider`: Handles communication with language models
- `IContextBuilder`: Formats prompts for the LLM
- `IPersistenceProvider`: Handles saving and loading agents
- `IDialogueUI`: Manages the user interface for conversations
- `IAIAgentManager`: Coordinates the creation and management of agents

### Implementation Module: `MaiMai.Implementation`

Concrete implementations of the core interfaces:

- `AIAgent`: Standard implementation of an AI agent
- `RAGMemoryProvider`: Memory system using Retrieval-Augmented Generation
- `LLMUnityProvider`: Integration with LLMUnity for language model access
- `DefaultContextBuilder`: Formats prompts with memory and conversation history
- `JsonPersistenceProvider`: Saves and loads agents using JSON
- `DialogueUIManager`: Handles dialogue UI rendering and interaction
- `AIAgentManager`: Manages all agents in the game

### Integration Module: `MaiMai.Integration`

Components for integrating AI agents into the game world:

- `AgentInteractionController`: Component that connects an agent to a game object

## 🚀 Getting Started

### Installation

1. Make sure you have LLMUnity installed in your Unity project
2. Import the MaiMai package into your project
3. Add the necessary prefabs to your scene

### Basic Setup

1. Add the `AIAgentManager` component to a GameObject in your scene
2. Assign a LLMCharacter component to the manager
3. Add the `DialogueUIManager` component to your UI canvas
4. Configure UI elements in the inspector

### Creating AI Agents

#### Via Script

```csharp
// Get a reference to the agent manager
var agentManager = AIAgentManager.Instance;

// Create a new agent
string agentName = "Shopkeeper";
string systemPrompt = "You are a friendly shopkeeper who sells potions and magical items...";
var agent = await agentManager.CreateAgent(agentName, systemPrompt);
```

#### Via Inspector

1. Add an `AgentInteractionController` component to an NPC GameObject
2. Set `_isFixedAgent` to true
3. Fill in `_fixedAgentName` and `_fixedAgentPrompt`
4. Optionally add memory files in the `_memoryFiles` array

### Configuring Memory

To add fixed memory to an agent:

```csharp
// Get the memory provider
var memoryProvider = GetMemoryProvider();

// Add fixed memory
string loreText = "The town of Eldervale was founded 300 years ago...";
await memoryProvider.StoreFixedMemoryAsync(loreText, agent.AgentId);
```

## 🔄 Agent Lifecycle

1. **Creation**: Agents are created with a name and system prompt
2. **Initialization**: The agent loads its memory and prepares for interaction
3. **Interaction**: Players communicate with the agent through dialogue
4. **Persistence**: Agent state is saved between game sessions
5. **Shutdown**: Agents are properly closed when the game ends

## 📝 Example Usage

### Creating an NPC in a Scene

```csharp
// Create an agent for an NPC
var agent = await agentManager.CreateAgent(
    "Village Elder",
    "You are a wise village elder with knowledge of ancient prophecies..."
);

// Add memory
await memoryProvider.StoreFixedMemoryAsync(
    "The prophecy speaks of a hero from beyond the mist...",
    agent.AgentId
);

// Associate with GameObject
var npcController = npcObject.GetComponent<AgentInteractionController>();
npcController.SetAgent(agent);
```

### Handling Player Interaction

When a player interacts with an agent:

1. The `AgentInteractionController` detects the interaction
2. It opens the dialogue UI through `DialogueUIManager`
3. Player messages are sent to the agent via `agent.Interact(userId, message)`
4. The agent processes the message, retrieves context, and generates a response
5. The response is displayed in the dialogue UI

## 🔌 Extending the System

### Creating Custom Memory Providers

Implement the `IMemoryProvider` interface:

```csharp
public class CustomMemoryProvider : IMemoryProvider
{
    // Implement the required methods
    public async Task StoreMessageAsync(MessageEntry message, string agentId)
    {
        // Your implementation
    }
    
    // ... other methods
}
```

### Creating Custom LLM Providers

Implement the `ILLMProvider` interface to support different language models:

```csharp
public class CustomLLMProvider : ILLMProvider
{
    // Implement the required methods
    public async Task<string> GetResponseAsync(string prompt, Dictionary<string, string> parameters = null)
    {
        // Your implementation
    }
    
    // ... other methods
}
```

## 📈 Performance Considerations

- **Memory Usage**: Large numbers of agents with extensive history can consume memory
- **Response Time**: Complex context retrieval can impact response time
- **Storage**: Persistent agents require disk space for saving state

## 🔒 Best Practices

1. **Limit Context Size**: Keep system prompts and memory concise
2. **Batch Save Operations**: Save agents during natural gameplay pauses
3. **Clean Up Resources**: Properly shut down agents when no longer needed
4. **Use Fixed Memory**: For static knowledge, use fixed memory instead of conversation history
5. **Cache Embeddings**: Reuse embeddings for frequently accessed content

## 🤝 Integration with LLMUnity

MaiMai leverages LLMUnity for core LLM functionality:

- **LLM Access**: Uses LLMCharacter for text generation
- **RAG System**: Uses LLMUnity's RAG component for memory retrieval
- **Embedding**: Uses LLMUnity for generating embeddings

## 📦 Dependencies

- Unity 2020.3 or newer
- LLMUnity
- TextMeshPro (for UI)
- Newtonsoft.Json (for serialization)

## 🔮 Future Development

- **Multi-Agent Conversations**: Allow agents to interact with each other
- **Emotion Modeling**: Add emotional states to agents
- **Action Systems**: Enable agents to perform in-game actions
- **Memory Pruning**: Automatically clean up old, less relevant memories
- **Voice Integration**: Support text-to-speech for agent responses