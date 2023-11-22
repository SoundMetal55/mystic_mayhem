using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//base script of all melee behaviors

public class MeleeSpellBehavior : MonoBehaviour
{
    public SpellSciptableObject spellData;

    public float destroyAfterSeconds;

    //Current stats
    protected float currentDamage;
    protected float currentSpeed;
    protected float currentMaxCooldownDuration;
    protected int currentPierce;

    private void Awake()
    {
        currentDamage = spellData.Damage;
        currentSpeed = spellData.Speed;
        currentMaxCooldownDuration = spellData.MaxCooldownDuration;
        currentPierce = spellData.Pierce;
    }

    public float GetCurrentDamage()
    {
        return currentDamage *= FindObjectOfType<CharacterStats>().currentPower;
    }

    protected virtual void Start()
    {
        Destroy(gameObject, destroyAfterSeconds);
    }

    protected virtual void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Enemy"))
        {
            Enemy enemy = col.GetComponent<Enemy>();
            enemy.TakeDamage(GetCurrentDamage()); //Make sure to use currentDamage instead of weaponData.damage in case of any damage multipliers
        }

    }

}
