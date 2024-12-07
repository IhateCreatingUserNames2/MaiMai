
# MaiMai AI Agent System

**MaiMai AI Agent System** allows players or Devs to spawn personal AI agents running through LLMUnity with a **custom AI agent name** and **custom AI agent system prompt**. Additionally, you can create **fixed AI agents** in the scene with customizable names and system prompts.

This system Uses LLMUnity for inference. It has RAG Implementation for Each Ai Agent. Each one has its own memory and its own rag. 

---
## HOW TO INSTALL MAI MAI WITHOUT LANG GRAPH 
  - Install LLMUNITY FOR UNITY ENGINE: - GitHub Repository: [https://github.com/undreamai/LLMUnity](https://github.com/undreamai/LLMUnity)
  - Install Newtonsoft JSON package : Enter Package Manager in Unity Engine -> Click Add by Name: com.unity.nuget.newtonsoft-json

  - DOWNLOAD MaiMaiNoLangGraph1.3.unitypackage From this GitHub Repository 
  - IMPORT MAIMAINOLANGGRAPH PACKAGE
   - ![image](https://github.com/user-attachments/assets/6be01eab-6dd6-495d-b817-d33a69bf3b00)

  - Setup GameObject in Scene
   - ![image](https://github.com/user-attachments/assets/8ed0e2c1-8d13-4436-a6d8-a5d58a3b3407)
   - ![image](https://github.com/user-attachments/assets/2ead7957-1b3c-4777-8fd1-06aa8adea330)
   - ![image](https://github.com/user-attachments/assets/96792c4c-7e95-4a0c-949a-bc42b39481fa)

   - ADD AI AGENT INTERACTION TO EVERY NPC - AI AGENT PREFAB
     - ![image](https://github.com/user-attachments/assets/eab77448-955d-4b6a-ae55-f0ae9dcda47c)
     - If its a FIXED AGENT(added by Dev in game scene), Toggle it ON. Add Name and Prompt. You can also add Memory Files, Text Files with Data that the system will Search for Relevant Information Based on User INPUT, add them to RAG and send them back in the Prompt Build. Check AIAgent.cs for the Prompt Build Structure. You can choose what to pass. Also Check the Llmcharacter.chat Instruction, because it passes down Chat History to the Prompt. You can Call llmcharacter.complete if you dont want Chat History to be sent. 


  - THE DEFAULT UI POSITION LOOK LIKE THIS:
   - ![image](https://github.com/user-attachments/assets/542a9262-03cf-4c17-ba0e-7c1c4ae2dfe1)

 - Configure OnClick Functions in Buttons:
  -  Select Ai Button  -> AiAgentManager. OnSelectAiClicked
  -  Despawn Button -> AiAgentManager.DespawnAllAgents
  - Create Agent Button -> AgentCreateUI.OnCreateAgentClicked 
  - PainelButton -> Button -> ToggleAiUIButton.ToggleAiUiButtonVisibility
  - SendButton -> PicoDialogue.OnSendButtonClicked 






## Requirements

### **LLMUnity**
- GitHub Repository: [https://github.com/undreamai/LLMUnity](https://github.com/undreamai/LLMUnity)

Install Newtonsoft JSON package : Add by Name com.unity.nuget.newtonsoft-json

### **PuerTS for JS and LangGraph**
- GitHub Repository: [https://github.com/Tencent/puerts/blob/master/doc/unity/en/install.md](https://github.com/Tencent/puerts/blob/master/doc/unity/en/install.md)
- Learn about Lang Graph: https://langchain-ai.github.io/langgraphjs/ - Lang Graph already have a built-in RAG system, Memory Saver, but the Code is using LLMUnity RAg System thru AiAgent.cs RetrieveRelevantContextAsync() Function, that is embebbed in the Final Prompt. You can Remove the Context from the Prompt and pass llmcharacter.complete() Instead of llmcharacter.chat() to remove Rag and Chat History. Edit the Final Prompt in AiAgent.cs. You can alternatively call LLMUnity RAG System thru Lang Graph to Handle the Memory Whenever needed. 

- **Note:** If you're not planning to use LangGraph, comment out the entire `InitializePuerts.cs`.

---

## With or Without Lang Graph - How to Remove LanG Graph 

This configuration of MaiMai runs with LangGraphJS. However, you can run MaiMai **without LangGraph** , by removing InitializePuerts.cs entirely and by editing the `OnSendButtonClicked()` function in `PicoDialogue.cs`:



### **Modify the following line:**
```csharp
InitializePuerts.Instance.ProcessUserInput(playerInput, currentAgent.AgentId);
```

### **Change it to:**
```csharp
RunAsyncResponse(playerInput);
```

---

## Installation

### **Download Contents:**

1. Extract `MaiMai` to your Unity **Assets** folder.
2. Extract `Resources` to your Unity **Assets/Resources** folder.

---


### **Default UI Configuration:**

For user interaction, MaiMai requires a UI with fields like:
- **AI Agent Name**
- **System Prompt**
- **Drop-down list of created agents**
- **Buttons to Spawn and Remove agents**

Utilities like changing models and voices are included but not fully integrated yet.
![image](https://github.com/user-attachments/assets/427dd1bd-f1e1-487d-8356-60fdd7f04aeb)

---

## Fixed NPCs
![image](https://github.com/user-attachments/assets/e3cdb1a6-186f-438a-b0a7-51fbd06ee024)

If adding fixed NPCs to your scene:
- The UI is **not required**.
- FIxed NPCS can have Fixed Memory: Upload Text Files. based on User Input, LLMCharacterMemory Will Perform A Search in those Text Files and Add Relevant Parts to the Rag thru rag.add, then retrieve it to the Prompt In AiAgent.cs. 
- Agent Name and System Prompt can be configured by attaching `AiAgentInteraction.cs` to the NPC GameObject and TOGGLING IS FIXED ON.

#

Fixed NPC

![image](https://github.com/user-attachments/assets/92d1fd35-2300-43c1-a564-d849066f6e1c)

#

DYNAMIC USER GENERATED NPC

![image](https://github.com/user-attachments/assets/e78beecb-2721-4cdf-9318-12a70c5dc2d9)


### **Important:**
- Every prefab must include the `AiAgentInteraction.cs` script.
- Toggle the **Fixed Agent** option in the inspector.

---

## Required Scene Components

Ensure the following components are added to the scene and properly configured in the Unity Inspector:

1. **`AgentCreateUI.cs`** (For user panel and Registering Ai Agents in the AI Manager. Ai Agent Interaction Also does this for Fixed Agents)
2. **`PicoDialogue.cs`** (Handles user input UI and LLM response display)
   - Add the `PicoDialogue.onSendButtonClicked()` method to the button's `onClick()` event.
   - Leave NPC configurations blank for dynamic agents.
3. **`LLmCharacter.cs`** (Leave NPC configurations blank for dynamic agents)
4. **`LLM.cs`**
   - Remember to download models and RAG models.
   - **Note:** Some Android utility scripts hardcode models (e.g., 1b and 3B). If adding more, adjust `LLMUnitySetupHelper.cs` accordingly.
5. **`AIManager.cs`**
6. **`AIAgentManager.cs`**
   - Configure in the Inspector.
   - **Current limitation:** Only supports one prefab model for user-created agents (dynamic support planned).
   - Ensure every prefab includes `AiAgentInteraction.cs`.
7. **`ModelSelector.cs`** (Optional: Load this script if you want to try the model selector.)
8. **`LLMCharacterMemoryManager.cs`**
9. **`RAG.cs`**
   - Configure `SearchType` DB Search, `Chunking` (e.g., Token Splitter), and `NumToken` (default: 10).
   - For custom configurations, consult `LLmCharacterMemoryManager.cs`.
10. **`InitializePuerts.cs`** (Initializes the LangGraph system)

---

## Chat Response - AI Agent Dialogue Display - Chat Room

To Retrieve the AI Response in MaiMai Check PicoDialogue RUnAsyncResponse() Function

                 `ShowDialogue(aiResponse);`
                `SendMessageToChat(aiResponse); // Optionally send to the chat system`

  call aiResponse in your function and it will display the Response where you want.
If you building Chat Bubbles, check the ChatHistory Options in LLMUnity to load Previous messages, if your own chat settings doesnt have a history function... 

---


## Using LangGraph

### **Setup:**

1. **Install Required Dependencies:**
   - Install **TypeScript**, **npx**, and **webpack**.
   - install PuerTS In Unity Engine https://github.com/Tencent/puerts/blob/master/doc/unity/en/install.md
   - https://docs.npmjs.com/downloading-and-installing-node-js-and-npm
   - Check `package.json` in `MaiMai/Package/webpack/langgraph-bundler`.
   - Extract node_modules.rar inside `MaiMai/Package/webpack/langgraph-bundler`.

2. **Edit LangGraph Code:**
   - Modify `index.ts` as needed.

3. **Bundle LangGraph:**
   - Run the following command in the `MaiMai/Package/webpack/langgraph-bundler` folder:
     ```bash
     npx webpack
     ```

4. **Update Project:**
   - Copy the generated `langgraph.bundle.mjs` from the `/dist/` folder.
   - Paste it into `ProjectFolder/Assets/Resources/`.

---

## LangGraph Logic

### **User Input Flow:**
1. `InitializePuerts.cs` initializes the JS environment.
2. Runs `testLangGraph.mjs -> langgraph.bundle.mjs -> index.ts`.
3. Imports LangGraph and initializes the environment.

### **Agent Creation:**
```javascript
const GraphState = Annotation.Root();
```

### **Process Flow:**
1. Processes user input, invokes LangGraph, and handles the final state.
2. Passes the final state to:
   ```csharp
   PicoDialogue.Instance.RunAsyncResponse(message);
   ```
3. `AIAgent` interacts with `RAG` to build the final prompt.

---

## Final Prompt Structure

The system constructs the following prompt:
```
Your name is: {AgentName}
Your Custom Prompt: {systemPrompt}

{contextSection}

As {AgentName}, please provide an appropriate response to the user's last message.

Respond in first person and do not include any conversation markers or role labels in your response.

Conversation History:
[ Chat history added from llmcharacter.chat feature ]
```

---

### **Static Completion( Completion Removes Chat History From Prompt:**
Replace:
```csharp
llmCharacter.Chat(message);
```
With:
```csharp
llmCharacter.Complete(message);
```
In `AiAgent.cs` for static completion.

---

## License

Refer to the respective repositories for licensing details of the required dependencies.
