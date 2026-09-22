# Modo offline

## Fluxos

- **Combate:** `Offline` -> `Game` -> `Credits`
- **Experiência completa:** `Offline` -> `GloboV2` -> `MedicalQuestions` -> `Credits`

`OfflineSession` guarda a escolha entre cenas e identifica quando os componentes compartilhados devem executar localmente.
`OfflineModeMenu` contém somente os métodos ligados aos botões da cena `Offline`.

O online continua usando Photon. No offline, player, pools, RPCs, projéteis, dano e mudanças de cena usam as alternativas locais implementadas nos mesmos componentes de gameplay.

## Setup da cena Offline

- Visual e menu derivados da `LoadingScene`.
- `ConnectionManager` e o rig antigo estão desativados somente nesta cena.
- `PlayerVR V3 (Offline Menu)` é o único rig jogável do menu.
- `MedicalQuestions` troca o rig médico antigo pelo `PlayerVR V3` somente no offline.
- Os botões `COMBATE` e `Experiência Completa` chamam `OfflineModeMenu`.
- A cena está incluída no `EditorBuildSettings`.
