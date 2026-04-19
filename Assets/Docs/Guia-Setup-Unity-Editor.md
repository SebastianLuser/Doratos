# Guia de Configuracion en Unity Editor — Doratos (PENDIENTES)

Todo lo demas ya fue creado via MCP y configurado manualmente.

---

## 1. Limpiar archivos obsoletos

Si todavia existen, eliminar:
- `Assets/Scenes/SampleScene.unity`
- `Assets/Resources/Prefabs/Bullet.prefab`
- Cualquier prefab de Player viejo (`Player.prefab`, `Player Health Bar.prefab`)

---

## 2. Test Rapido (Checklist)

### Test 1: Lobby
1. Abrir `Lobby.unity`
2. Play
3. Deberia verse el boton "CONECTAR" y texto de estado
4. Click en CONECTAR → deberia cambiar a "Conectando..." → "Buscando sala..." → "Esperando oponente (1/2)"

### Test 2: Dos Clientes
1. **Build and Run** (File > Build And Run)
2. Abrir el editor con Play tambien
3. Ambos clickean CONECTAR
4. Cuando los dos estan en la sala, deberian cargar la escena Arena
5. Dos capsulas deberian aparecer en posiciones opuestas

### Test 3: Gameplay
1. WASD para mover
2. Mouse para apuntar
3. Click derecho para escudo
4. Left Shift para dash
5. Caminar sobre la lanza para recogerla
6. Click izquierdo para tirar la lanza
7. Verificar que el danio funciona y aparece pantalla de victoria/derrota

### Test 4: Desconexion
1. Con dos clientes en partida, cerrar uno
2. El otro deberia ver "Tu oponente se desconecto - Victoria"
3. Click en "Volver al Lobby" deberia funcionar
