using System.Collections;
using UnityEngine;

public class IntervalSwitch : MonoBehaviour
{
    [SerializeField] private Behaviour behaviour;
    [SerializeField] private float switchTime = 1;
    [SerializeField] private float postponeTime = 0;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(postponeTime);
        do
        {
            yield return new WaitForSeconds(switchTime);
            behaviour.enabled = !behaviour.enabled;
        }while (true);
    }
}
