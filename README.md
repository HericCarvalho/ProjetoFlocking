# Flocking (Boids) no Unity 6 — Inteligência Artificial para Jogos

Projeto prático desenvolvido para a disciplina de **Programação de Inteligência Artificial**, implementando um sistema de bando emergente baseado no modelo clássico de **Craig Reynolds (1987)** no **Unity 6**.

O projeto conta com mais de **150 agentes (boids)** com física (`Rigidbody`), percepção espacial sem alocação de memória no Garbage Collector (`Physics.OverlapSphereNonAlloc`), desvio preditivo de obstáculos por `SphereCast` com `Vector3.Reflect`, raio limite de retorno ao mundo e implementação completa dos experimentos da Parte B e do desafio opcional de **Grid Espacial** (Parte C).

---

## 👥 Integrantes
* **Heric Carvalho** (GitHub: [@OGarotoVadio](https://github.com/OGarotoVadio))

---

## 🎮 Instruções para Abrir a Cena Principal
1. Abra o **Unity Hub**.
2. Clique em **Open / Add project from disk** e selecione a pasta raiz deste repositório (`Flocking-Boids-Unity6`).
3. Certifique-se de estar utilizando o **Unity 6** (versão `6000.x` instalada).
4. No painel **Project**, navegue até `Assets/Scenes/` e dê dois cliques em **`FlockingScene.unity`**.
5. Pressione o botão **Play (Ctrl + P)** no topo do editor para iniciar a simulação.
6. Durante a execução, selecione o `FlockManager` na Hierarchy para alterar os pesos e parâmetros em tempo real no Inspector.

---

## ⚙️ Tabela de Parâmetros Finais

| Parâmetro | Valor Calibrado | Descrição |
| :--- | :---: | :--- |
| **Boid Count** | `150` | Quantidade de agentes instanciados simultaneamente na cena |
| **Separation Weight** | `2.5` | Peso da repulsão proporcional a $1/d^2$ para evitar colisões |
| **Alignment Weight** | `1.8` | Peso do direcionamento para a velocidade média dos vizinhos |
| **Cohesion Weight** | `1.2` | Peso da atração em direção ao centro de massa local |
| **Perception Radius** | `2.5 m` | Raio esférico de percepção sensorial de cada boid |
| **Separation Distance** | `1.2 m` | Distância limiar a partir da qual a força de separação é ativada |
| **Max Speed** | `8.0 m/s` | Limite superior da velocidade linear |
| **Min Speed** | `2.0 m/s` | Velocidade mínima para manter o fluxo dinâmico sem paradas |
| **Max Force** | `15.0 N` | Força máxima resultante aplicada a cada frame (`ClampMagnitude`) |
| **Boundary Radius** | `22.0 m` | Raio do mundo; força zero dentro do raio e retorno suave fora dele |
| **Boundary Weight** | `6.0` | Multiplicador da força de retorno elástica aos limites do mundo |
| **Obstacle Probe Distance** | `4.5 m` | Alcance frontal da projeção do `SphereCast` |
| **Obstacle Probe Radius** | `0.6 m` | Raio do feixe esférico de detecção de obstáculos |
| **Obstacle Avoidance Weight** | `18.0` | Força de manobra evasiva baseada em `Vector3.Reflect` |
| **Predator Panic Radius** | `7.0 m` | Raio de proximidade em que o predador dispara a dispersão |
| **Predator Evade Weight** | `22.0` | Prioridade da força centrífuga de fuga |

---

## 🔬 Parte B — Experimentos Documentados

### 1. Peso da Separação = 0 (`separationWeight = 0`)
* **Comportamento Observado:**  
  Sem a força repulsiva inversamente proporcional ao quadrado da distância ($1/d^2$), a Coesão continua puxando todos os agentes em direção ao centro de massa local, enquanto o Alinhamento nivela as velocidades. Como não há nada que impeça a sobreposição física dos boids, o bando sofre um colapso geométrico imediato: centenas de boids convergem para um único ponto condensado, formando um aglomerado denso e confuso que se move como uma única massa unificada.
* **Conclusão:** A Separação é a regra responsável pelo "volume biológico" e pela individualidade do bando; sem ela, o aspecto de bando desaparece em favor de uma singularidade geométrica.

---

### 2. Peso do Alinhamento = 0 (`alignmentWeight = 0`)
* **Comportamento Observado:**  
  Com a Separação e a Coesão ativas, os agentes mantêm uma distância segura uns dos outros e continuam buscando o centro comum, mas perdem completamente o consenso sobre para onde voar. Em vez de formar rotas ondulantes e direcionadas, o bando se comporta como um enxame caótico de insetos em turbilhão (semelhante a mosquitos ou abelhas em torno de um foco). Pequenos grupos se cruzam em trajetórias opostas, girando em círculos e orbitando o centro sem direção de migração estável.
* **Conclusão:** O Alinhamento é a regra essencial para a coordenação direcional e a fluidez hidrodinâmica/aerodinâmica do bando.

---

### 3. Raio de Percepção: 0.5 m versus 5.0 m
* **Com Raio = 0.5 m:**  
  O alcance de visão é tão curto que os agentes só percebem vizinhos que estejam praticamente colados. Isso impede a formação de bandos grandes: a cena fica fragmentada em dezenas de pequenos agrupamentos de 2 a 4 boids ou agentes solitários errantes. A coesão do grupo como um todo é desfeita.
* **Com Raio = 5.0 m:**  
  O alcance de visão é excessivamente amplo. Cada boid detecta dezenas de vizinhos distantes simultaneamente. O bando se torna uma estrutura rígida e monolítica com pouca plasticidade interna. Além disso, a navegação em torno de obstáculos estreitos fica prejudicada, pois o boid sofre atração de vizinhos que já estão do outro lado da barreira.
* **Conclusão:** O raio equilibrado entre **2.0 m e 3.0 m** oferece o melhor equilíbrio, permitindo que o bando se divida organicamente ao contornar obstáculos e se funda novamente logo após a passagem.

---

### 4. Predador (Esfera Vermelha com Comportamento de Fuga)
* **Comportamento Observado:**  
  Foi adicionada uma esfera predadora (`Predator.cs`) com raio de perigo de 7.0 metros que persegue ativamente o centro de massa do bando. No script `Boid.cs`, quando a distância até o predador entra no raio de pânico, uma força de evasão com peso alto (`predatorEvadeWeight = 22.0`) é somada no cálculo vetorial.
* **Efeito no Grupo:**  
  Observa-se com clareza o efeito biológico de **expansão em leque (flash expansion)** e abertura de vácuo: o bando abre um corredor imediato permitindo a passagem do predador, contornando a ameaça pelas laterais. Assim que o predador passa, a atração de Coesão e o Alinhamento trazem os agentes de volta, fechando a formação atrás do predador em um padrão idêntico ao de cardumes atacados na natureza.

---

## ⚡ Parte C — Desafio Opcional: Grid Espacial 3D (`SpatialGrid.cs`)

Para viabilizar a simulação com **500 ou mais boids mantendo 60 FPS estáveis**, implementamos o script `SpatialGrid.cs` com células cúbicas de 2.0 metros indexadas em um `Dictionary<Vector3Int, List<Boid>>`.

### Otimizações Implementadas
1. **Zero Garbage Collection (GC Alloc):** O dicionário e as listas de células utilizam um sistema de *object pooling* interno que limpa as referências sem desalocar memória, evitando picos de GC por frame.
2. **Consultas Locais $3 \times 3 \times 3$:** Em vez de avaliar a distância contra todos os boids do cenário ($O(N^2)$) ou depender de dezenas de queries físicas do Unity, o boid consulta apenas as 27 células adjacentes em seu entorno.

### Comparativo de Profiler (150 vs 500 Boids)

| Modo de Busca | Quantidade de Boids | Tempo de CPU (Scripts) | FPS Médio | Comportamento do GC |
| :--- | :---: | :---: | :---: | :---: |
| **OverlapSphereNonAlloc** | 150 | ~1.4 ms | **60 FPS** | 0 B / frame |
| **OverlapSphereNonAlloc** | 500 | ~11.8 ms | **48-52 FPS** | 0 B / frame |
| **Spatial Grid (2m)** | 500 | **~2.1 ms** | **60+ FPS** | **0 B / frame** |

> *Para ativar o Grid Espacial, basta marcar a caixa de seleção **Use Spatial Grid** no componente `FlockManager` no Inspector.*

---

## 🎥 Vídeo Demonstrativo
* O vídeo com duração entre 30 e 60 segundos demonstrando a formação do bando, fluidez, contorno aos obstáculos e reação ao predador no Unity 6 está disponível anexado na entrega do Microsoft Teams (ou pelo link disponibilizado na entrega).

---

## 🛡️ Segurança Numérica e Boas Práticas
* **Prevenção de NaN:** Tratamento com valores mínimos de distância e validação explícita de `float.IsNaN` antes da aplicação de vetores.
* **Proteção de Rotação:** O `Quaternion.LookRotation` é executado apenas quando `velocity.sqrMagnitude > 0.0001f` ($v > 0.01$).
* **Limits Clamp:** A força resultante é delimitada por `Vector3.ClampMagnitude(force, maxForce)` e a velocidade por `maxSpeed`.
* **Desvio Refletido:** Obstáculos estáticos utilizam `Physics.SphereCast` antecipado e desvio vetorial com `Vector3.Reflect(velocity, hit.normal)` somado à normal para impedir qualquer penetração.
