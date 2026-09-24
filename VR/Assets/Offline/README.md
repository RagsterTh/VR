# Modo offline

Tudo que é exclusivo do offline fica em `Assets/Offline/`. O modo online continua usando Photon sem mudança de comportamento.

## Como usar

1. Rode uma vez **Tools > Offline > Configurar modo offline e cura**. O menu cria/atualiza as cenas offline, a cura no `PlayerVR V3` e o Build Settings (pode rodar de novo quando quiser).
2. Abra `Assets/Offline/Scenes/Offline.unity`.
3. No objeto com `OfflineModeMenu`, escolha **Mode** (`CombatOnly` ou `FullExperience`).
4. Aperte Play. Com **Start Automatically** ligado, o modo inicia após o **Auto Start Delay**. No headset, o painel na frente do jogador também permite escolher ou iniciar com o controle (raio/poke).

Qualquer cena de `Assets/Offline/Scenes` também pode receber Play direto: ela entra em modo offline sozinha.

## Fluxos

- **Combate:** `Offline` -> `Offline_Combat` (cópia de `Game`) -> `Offline_Credits`
- **Experiência completa:** `Offline` -> `Offline_GloboV2` (saguão e depois combate na mesma cena) -> `Offline_Medical` -> `Offline_Credits`

## Peças

| Arquivo | Função |
|---|---|
| `Scripts/OfflineSession.cs` | Guarda o modo escolhido, detecta cena offline (caminho `Assets/Offline/`), mapeia nomes de cena originais para as cópias e faz as trocas de cena com fade. |
| `Scripts/OfflineModeMenu.cs` | Cena de entrada: modo no Inspector, auto início e painel VR. |
| `Scripts/OfflineSceneBootstrap.cs` | Primeiro script de cada cena offline: força o caminho VR, desliga objetos só online (Photon, câmera do operador de PC, detector de VR) e faz o fade-in. |
| `Scripts/OfflinePlayerRig.cs` | Substitui o `ResetPosition` no offline: um rig e um AudioListener, orientação do headset alinhada ao início e reaplicada ao recentralizar. |
| `Scripts/OfflineScreenFade.cs` | Fade preto preso à câmera VR entre as cenas. |
| `Editor/OfflineModeSetup.cs` | Ferramenta do menu Tools > Offline. |

Os scripts de gameplay compartilhados (`Assets/Scripts`) têm um ramo `OfflineSession.IsOffline` que troca RPC/instanciação em rede/ownership pela chamada local equivalente.

## Cura (online e offline)

`PlayerHeal` no `PlayerVR V3`: botão **A** (controle direito) ou **X** (esquerdo), ou **H** no teclado para teste no Editor. O HUD `HealAbility` fica acima da barra de vida, com a letra do botão em cima e o cooldown radial com os segundos restantes. Os valores padrão (25 de vida e 20 s de cooldown) podem ser ajustados no Inspector. No online, a cura é sincronizada por RPC, como o dano.
