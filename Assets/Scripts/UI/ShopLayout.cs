using System;
using TMPro;
using UnityEngine;
using Zenject;

public class ShopLayout : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI m_dialogueText;

    GameObject m_player;
    AudioSource m_buySound;

    [Inject]
    UIController m_UI;
    [Inject]
    LevelBuilder m_lvlBuilder;
    [Inject]
    PlayerInput m_input;

    bool[] m_clicked = new bool[5];
    int[] m_itemsCount = { 3, 2, 2, 2, 1 };
    int[] m_prices = { 1000, 1500, 2000, 2000, 2500 };
    string[][] m_dialogueTexts =
    {
        new string[]{
            "Adds an extra health heart. ",
            "Adiciona saúde do coração extra. ",
            "Добавляет дополнительное сердце здоровья. ",
            "Agrega salud extra al corazón. ",
            "Ekstra bir sağlık kalbi ekler. "
        },
        new string[]{
            "Decreases the dash cooldown time. ",
            "Reduz o tempo de recarga do puxão. ",
            "Уменьшает время перезарядки рывка. ",
            "Reduce el tiempo de recarga del tirón. ",
            "Sarsıntının yeniden yükleme süresini azaltır. "
        },
        new string[]{
            "Increases the light attack's damage. ",
            "Aumenta o dano de ataque leve. ",
            "Увеличивает урон легкой атаки. ",
            "Aumenta el daño de ataque ligero. ",
            "Hafif saldırı hasarını artırır. "
        },
        new string[]{
            "Increases the heavy attack's damage. ",
            "Aumenta o dano de ataque pesado. ",
            "Увеличивает урон тяжелой атаки. ",
            "Aumenta el daño de ataque pesado. ",
            "Ağır saldırı hasarını artırır. "
        },
        new string[]{
            "Adds an extra jump. You will be able to make a triple jump. ",
            "Adiciona um salto extra. Você será capaz de fazer um salto triplo. ",
            "Добавляет дополнительный прыжок. Вы сможете совершить тройной прыжок. ",
            "Agrega un salto extra. Podrás hacer un triple salto. ",
            "Ekstra bir sıçrama ekler. Sen üçlü bir sıçrama yapmak mümkün olacak. "
        }
    };

    string[][] m_canBuyText =
    {
        new string[]
        {
            "Press ENTER to buy.",
            "Pressione ENTER para comprar.",
            "Нажмите ENTER, чтобы купить.",
            "Pulse ENTER para comprar.",
            "Satın almak için ENTER'a basın."
        },
        new string[]
        {
            "Press A to buy.",
            "Pressione A para comprar.",
            "Нажмите A, чтобы купить.",
            "Pulse A para comprar.",
            "Satın almak için A'a basın."
        }
    };

    string[] m_cantBuyText =
    {
        "But you don't have enough money, beggar.",
        "Mas não tens dinheiro suficiente, mendigo.",
        "Но у тебя недостаточно денег, нищий.",
        "Pero no tienes suficiente dinero, mendigo.",
        "Ama yeterli paran yok dilenci."
    };

    string[] m_greetingText =
    {
        "Hello, Stranger! Welcome to my shop! What would you like to purchase?",
        "Olá, Estranho! Bem-vindo à minha loja! O que você gostaria de comprar?",
        "Привет, Путник! Добро пожаловать в мой магазин! Что бы ты хотел приобрести?",
        "¡Hola, Forastero! Bienvenido a mi tienda! ¿Qué le gustaría comprar?",
        "Merhaba Yabancı! Benim dükkana hoşgeldiniz! Ne satın almak istersiniz?"
    };

    public int AllPrices { get; private set; }


    public float GetLowestPrice()
    {
        for (int i = 0; i < m_prices.Length; i++)
        {
            if (m_itemsCount[i] > 0)
                return m_prices[i];
        }

        return float.MaxValue;
    }

    void InitializePrices()
    {
        m_prices = new int[] { 1000, 1500, 2000, 2000, 2500 };
        for (int i = 0; i < m_itemsCount.Length; i++)
        {
            AllPrices += m_itemsCount[i] * m_prices[i];
        }
    }

    private void Start()
    {
        m_player = GameObject.FindGameObjectWithTag("Player");
        m_buySound = GetComponent<AudioSource>();
        m_dialogueText.text = m_greetingText[m_UI.CurrentLanguage];
        InitializePrices();
    }

    public void Greet()
    {
        m_dialogueText.text = m_greetingText[m_UI.CurrentLanguage];
    }

    public void ShowItemText(int index)
    {
        m_dialogueText.text = m_dialogueTexts[index][m_UI.CurrentLanguage];
        m_dialogueText.text += m_UI.GetMoney() >= m_prices[index] ? m_canBuyText[m_input.GetCurrentDeviceType()=="Gamepad"?1:0][m_UI.CurrentLanguage] : m_cantBuyText[m_UI.CurrentLanguage];
        for (int i = 0; i < m_clicked.Length; i++)
        {
            m_clicked[i] = false;
        }
        m_clicked[index] = true;
    }

    void Buy(int index, TextMeshProUGUI price, Action func)
    {
        if (m_itemsCount[index] <= 0)
            return;
        if (!m_clicked[index])
        {
            m_dialogueText.text = m_dialogueTexts[index][m_UI.CurrentLanguage];
            m_dialogueText.text += m_UI.GetMoney() >= m_prices[index] ? m_canBuyText[m_input.GetCurrentDeviceType() == "Gamepad" ? 1 : 0][m_UI.CurrentLanguage] : m_cantBuyText[m_UI.CurrentLanguage];
            for (int i = 0; i < m_clicked.Length; i++)
            {
                m_clicked[i] = false;
            }
            m_clicked[index] = true;
        }
        else if (m_UI.GetMoney() >= m_prices[index])
        {
            m_buySound.Play();
            m_UI.AddMoney(-m_prices[index]);
            m_dialogueText.text = m_greetingText[m_UI.CurrentLanguage];
            func();
            m_clicked[index] = false;
            m_itemsCount[index]--;
            if (m_itemsCount[index] <= 0)
            {
                price.text = "SOLD";
            }
        }
    }

    public void AddHealth(TextMeshProUGUI price)
    {
        Action func = m_UI.AddHeart;
        func += m_player.GetComponent<Damagable>().IncreaseHealth;
        Buy(0, price, func);
    }


    public void AddDash(TextMeshProUGUI price)
    {
        Buy(1, price, m_player.GetComponent<PlayerController>().DecreaseDashCooldown);
    }

    public void AddLightDamage(TextMeshProUGUI price)
    {
        Buy(2, price, m_player.transform.GetChild(0).GetComponent<AttackListener>().IncreaseDamage);
    }

    public void AddHeavyDamage(TextMeshProUGUI price)
    {
        Buy(3, price, m_player.transform.GetChild(1).GetComponent<AttackListener>().IncreaseDamage);
    }

    public void AddJump(TextMeshProUGUI price)
    {
        Action func = m_player.GetComponent<PlayerController>().AddJump;
        func += m_lvlBuilder.SetTripleJump;
        Buy(4, price, func);
    }
}
