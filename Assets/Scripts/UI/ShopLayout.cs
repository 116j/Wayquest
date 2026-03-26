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

    //Количество товаров
    int[] m_itemsCount = { 3, 2, 2, 2, 1 };
    //Цены товаров
    int[] m_prices = { 1000, 1500, 2000, 2000, 2500 };
    //Описание каждого товара
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

    string[][] m_closeShopText =
    {
        new string[]
        {
            "Press F to close the shop.",
            "Pressione F para fechar a loja.",
            "Нажмите F, чтобы закрыть магазин.",
            "Presiona F para cerrar la tienda.",
            "Dükkânı kapatmak için F'ye basın."
        },
        new string[]
        {
            "Press B to close the shop.",
            "Pressione B para fechar a loja.",
            "Нажмите B, чтобы закрыть магазин.",
            "Presiona B para cerrar la tienda.",
            "Dükkânı kapatmak için B'ye basın."
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
    //Стоимость всех товаров
    public int AllPrices { get; private set; }
    /// <summary>
    /// Самая маленькая ценна товара в магазине
    /// </summary>
    /// <returns></returns>
    public float GetLowestPrice()
    {
        for (int i = 0; i < m_prices.Length; i++)
        {
            if (m_itemsCount[i] > 0)
                return m_prices[i];
        }

        return float.MaxValue;
    }
    /// <summary>
    /// Ставит цены на товары
    /// </summary>
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
    /// <summary>
    /// Приветственная реплика продавца
    /// </summary>
    public void Greet()
    {
        m_dialogueText.text = m_greetingText[m_UI.CurrentLanguage]
            + "\n\r" + m_closeShopText[m_input.GetCurrentDeviceType() == "Gamepad" ? 1 : 0][m_UI.CurrentLanguage];
    }
    /// <summary>
    /// Показывает описание товара
    /// </summary>
    /// <param name="index">номер товара</param>
    public void ShowItemText(int index)
    {
        m_dialogueText.text = m_dialogueTexts[index][m_UI.CurrentLanguage];
        m_dialogueText.text += (m_UI.GetMoney() >= m_prices[index] ?
            m_canBuyText[m_input.GetCurrentDeviceType() == "Gamepad" ? 1 : 0][m_UI.CurrentLanguage] : m_cantBuyText[m_UI.CurrentLanguage])
            + "\n\r" + m_closeShopText[m_input.GetCurrentDeviceType() == "Gamepad" ? 1 : 0][m_UI.CurrentLanguage];
    }
    /// <summary>
    /// Купить товар
    /// </summary>
    /// <param name="index">номер товара</param>
    /// <param name="price">текст цены товара</param>
    /// <param name="func">применение  товара к игроку</param>
    void Buy(int index, TextMeshProUGUI price, Action func)
    {
        if (m_itemsCount[index] <= 0)
            return;
        if (m_UI.GetMoney() >= m_prices[index])
        {
            m_buySound.Play();
            m_UI.AddMoney(-m_prices[index]);
            Greet();
            func();
            m_itemsCount[index]--;
            if (m_itemsCount[index] <= 0)
            {
                price.text = "SOLD";
            }
        }
    }
    /// <summary>
    /// Добавляет + 1 здоровье игроку
    /// </summary>
    /// <param name="price"></param>
    public void AddHealth(TextMeshProUGUI price)
    {
        Action func = m_UI.AddHeart;
        func += m_player.GetComponent<Damagable>().IncreaseHealth;
        Buy(0, price, func);
    }
    /// <summary>
    /// Уменьшает время презагрузки рывка
    /// </summary>
    /// <param name="price"></param>
    public void AddDash(TextMeshProUGUI price)
    {
        Buy(1, price, m_player.GetComponent<PlayerController>().DecreaseDashCooldown);
    }
    /// <summary>
    /// Увеличивает урон лекгой актаки на 1
    /// </summary>
    /// <param name="price"></param>
    public void AddLightDamage(TextMeshProUGUI price)
    {
        Action func = m_player.transform.GetChild(0).GetComponent<AttackListener>().IncreaseDamage;
        func += m_lvlBuilder.IncreaseBossHealth;
        Buy(2, price, func);
    }
    /// <summary>
    /// Увеличивает урон тяжелой актаки на 1
    /// </summary>
    /// <param name="price"></param>
    public void AddHeavyDamage(TextMeshProUGUI price)
    {
        Buy(3, price, m_player.transform.GetChild(1).GetComponent<AttackListener>().IncreaseDamage);
    }
    /// <summary>
    /// Добавляет 1 макс прыжок игроку
    /// </summary>
    /// <param name="price"></param>
    public void AddJump(TextMeshProUGUI price)
    {
        Action func = m_player.GetComponent<PlayerController>().AddJump;
        func += m_lvlBuilder.SetTripleJump;
        Buy(4, price, func);
    }
}
