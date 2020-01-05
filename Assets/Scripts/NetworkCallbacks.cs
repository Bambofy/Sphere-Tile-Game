using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[BoltGlobalBehaviour]
public class NetworkCallbacks : Bolt.GlobalEventListener
{
    public static BoltEntity Map;
    public static BoltEntity Chat;
    public static uint IP;

    public override void SceneLoadLocalDone(string scene)
    {
        // create map.
        Map = BoltNetwork.Instantiate(BoltPrefabs.Map);

        // create chat.
        Chat = BoltNetwork.Instantiate(BoltPrefabs.Chat);

        // spawn ourselves into the world.
        BoltEntity player = BoltNetwork.Instantiate(BoltPrefabs.Player);

        player.transform.SetParent(GameObject.FindGameObjectWithTag("FOLDER_PLAYERS").transform);
    }

    public override void Connected(BoltConnection connection)
    {
        IP = BoltNetwork.UdpSocket.WanEndPoint.Address.Packed;
    }
}
