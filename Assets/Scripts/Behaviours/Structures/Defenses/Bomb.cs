using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : DefenseBehaviour
{
    private RaycastHit[] hits;

    protected void Explode()
    {
        hits = Physics.SphereCastAll(transform.position, attackRange, transform.forward, attackRange, layerMask);
        for (int i = 0; i < hits.Length; i++)
        {
            EnemyBehaviour eb;
            if (hits[i].collider.TryGetComponent<EnemyBehaviour>(out eb))
            {
                eb.Hurt(damage);
            }
        }
    }
}

