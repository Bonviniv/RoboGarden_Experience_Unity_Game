# PGMVproject — RoboGarden Experience 🌱🤖

**Simulação interativa de plantas procedurais + robótica**  
Autores: **Vitor Barbosa (105248)**, **Tiago Afonso (104624)**, **Pedro Cruz (99379)**  
Demonstrações: https://www.youtube.com/playlist?list=PLyA90E6LMq-4-M9vp_2Kjqof8fvdqNLss

---

🎯 Visão rápida  
PGMVproject é uma experiência 3D imersiva construída em Unity que combina geração procedural de plantas com um sistema interativo de manipulação por um robô/personagem. O projeto evidencia técnicas de modelação procedural, interfaces claras para exploração de parâmetros e uma cena pronta para demonstração.

---

✨ Destaques que impressionam recrutadores e avaliadores
- Geração procedural de plantas visualmente expressiva — manipule regras e iterações para criar morfologias distintas.
- Interface interativa para visualização passo-a-passo do crescimento (sliders e controles em runtime).
- Sistema de interação intuitivo: colocar, pegar e transportar plantas entre módulos (bancada, armário, vasos).
- Cenário com áudio ambiente e sinais sonoros ligados a ações — aumenta a imersão nas demos.
- Arquitetura modular e extensível: separação clara entre PlantSystem, Interaction, Audio e UI, facilitando manutenção e evolução.
- Preparado para demonstrações públicas: cena otimizada, prefabs reutilizáveis e documentação para uso rápido.

---

🧠 Como funciona (resumo técnico)
1. Gerador de plantas (PlantSystem)
   - Regras paramétricas que geram ramos, folhas e flores em iterações — permitindo variações morfológicas.
2. Sistema de interação (Interaction)
   - Player/robô faz pick & place de prefabs; modos de interação controlam permissões e animações.
3. UI & Visualização
   - Sliders em runtime para controlar iterações, escala e outros parâmetros de geração.
4. Áudio & Feedback
   - Sons contextuais vinculados a movimentos e ações para reforçar as interações.

---

🛠️ Tecnologias
- Unity (motor de jogo)
- C# (scripts organizados por módulos)
- Prefabs, sistema de UI integrado e áudio nativo do Unity
- Ferramentas recomendadas: Unity Hub, Visual Studio / Rider

---

⚡ Execução rápida (para apresentar em minutos)
1. Clone:
   git clone https://github.com/Bonviniv/PGMVproject.git
2. Abra o projeto com Unity Hub.
3. Abra a cena de demonstração em Assets/Scenes (scene: DemoScene).
4. Pressione Play no Editor e use a UI da cena para gerar plantas e interagir.

Controles típicos na demo:
- Movimento: W A S D
- Olhar: mouse
- Interagir: E (pegar/colocar)
- UI: sliders para ajustar o crescimento e iterações

---

📁 Estrutura (visão geral)
- Assets/Scenes — cenas de demonstração
- Assets/Scripts/PlantSystem — geração procedural
- Assets/Scripts/Interaction — lógica de pick & place e modos do robô
- Assets/Prefabs — plantas, vasos, robô
- Assets/Audio — efeitos sonoros e ambiente
- docs/ — materiais de apoio e screenshots (sugerido)

---


👥 Equipa
- Vitor Barbosa — 105248  
- Tiago Afonso — 104624  
- Pedro Cruz — 99379

---
