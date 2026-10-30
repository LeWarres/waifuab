"""Tzmil's authored dialogue, exported to the game's existing localization TSV."""
import json
from pathlib import Path

chapters = [
    {
        'title': ['Una hermana desde la cima', 'A sister on the summit', 'Uma irmã no topo', '頂から見守る妹'],
        'intro': [
            ['Soy Tzmil, una amorosa hermana menor. Lo de amorosa es importante: explica casi todas las malas decisiones que vienen después.', 'I am Tzmil, a loving little sister. The loving part matters: it explains almost all the bad decisions that follow.', 'Sou Tzmil, uma irmã caçula amorosa. A parte amorosa importa: explica quase todas as más decisões que vêm depois.', '私はツミル。兄思いの妹です。兄思い、というのが大事。これからする間違いのほとんどは、それが理由だから。'],
            ['Mi hermano se pasaba el tiempo de fiesta y molestando a los humanos. Nuestro padre acabó cansándose y lo mandó directamente a vivir con ellos.', 'My brother spent his days partying and bothering humans. Our father finally got fed up and sent him straight down to live with them.', 'Meu irmão vivia em festas e incomodando os humanos. Nosso pai acabou perdendo a paciência e o mandou morar com eles.', '兄は宴会ばかりして、人間を困らせていました。とうとう父が怒って、兄を人間の世界へ放り出したんです。'],
            ['A mí me pidió que lo cuidara. En realidad, solo quería que trabajara un poco y entendiera el valor de la vida. Una tarea sencilla... si tu hermano fuera otro.', 'He asked me to look after him. Really, he just wanted my brother to work a little and learn the value of life. A simple task... if I had a different brother.', 'Ele me pediu para cuidar dele. Na verdade, só queria que meu irmão trabalhasse um pouco e entendesse o valor da vida. Uma tarefa simples... se meu irmão fosse outro.', '父は私に兄の見守りを頼みました。少し働いて、命の大切さを学んでほしかっただけ。簡単な任務です。兄が別の人なら。'],
            ['Así terminé como guardiana de un volcán. Desde la cima podía vigilarlo sin estarle diciendo cada cinco minutos que se portara bien.', 'That is how I became guardian of a volcano. From the summit I could watch him without telling him to behave every five minutes.', 'Foi assim que virei guardiã de um vulcão. Do topo eu podia vigiá-lo sem pedir que se comportasse a cada cinco minutos.', 'それで私は火山の守り手になりました。頂上からなら、五分おきに「いい子にして」と言わずに見守れます。'],
            ['Había preparado un lugar tranquilo, con buenas vistas. Lo que no preparé fue un protocolo para hermanos que llegan cargando una roca verde capaz de hacer explotar medio mundo.', 'I had found a quiet spot with a lovely view. What I had not prepared was a procedure for brothers arriving with a green rock that could blow up half the world.', 'Eu tinha preparado um lugar tranquilo, com uma bela vista. Só não preparei um protocolo para irmãos chegando com uma pedra verde capaz de explodir metade do mundo.', '静かで眺めもいい場所でした。でも、世界の半分を吹き飛ばす緑の石を抱えた兄が来る場合の手順までは、用意していません。'],
            ['Y, por supuesto, vino a tirarla en MI volcán. Padre, si esto era para enseñarle a trabajar, creo que la que está haciendo horas extra soy yo.', 'And of course he came to throw it into MY volcano. Father, if this was meant to teach him to work, I think I am the one doing overtime.', 'E, claro, ele veio jogá-la no MEU vulcão. Pai, se era para ensinar ele a trabalhar, acho que quem está fazendo hora extra sou eu.', 'しかも投げ込む先は、私の火山。父さん、働くことを教える任務なら、残業しているのは私のほうです。'],
        ],
        'before': [
            ['¡No intentes detenerme, hermana! ¡Si no puedo ser un héroe, seré el villano!', 'Do not try to stop me, sister! If I cannot be a hero, I will be the villain!', 'Não tente me impedir, irmã! Se não posso ser um herói, serei o vilão!', '止めるな、妹よ！英雄になれないなら悪役になってやる！'],
            ['Volar medio mundo no hará que padre te quiera más. Estás enfadado con el gobierno porque no te lanzó al espacio, no con toda la gente que vive aquí.', 'Blowing up half the world will not make Father love you more. You are angry at the government for not launching you into space, not at everyone living here.', 'Explodir metade do mundo não vai fazer o pai te amar mais. Sua raiva é do governo que não te lançou ao espaço, não de todos que moram aqui.', '世界を半分壊しても、父さんの愛は増えないよ。怒っているのは宇宙に送らなかった政府でしょ。ここで暮らす人たちじゃない。'],
            ['¡Entonces demuéstrame que tienes razón! ¡Acepta mi holoajedrez!', 'Then prove you are right! Face me in holo-chess!', 'Então prove que tem razão! Encare meu holo-xadrez!', 'なら正しいと証明しろ！ホロチェスで勝負だ！'],
            ['Acepto. Pero la roca se queda fuera del tablero. Y del cráter.', 'I accept. But the rock stays off the board. And out of the crater.', 'Aceito. Mas a pedra fica fora do tabuleiro. E da cratera.', 'いいよ。でも石は盤の外。火口の外にも置いてね。'],
        ],
        'after': [
            ['Está bien... No voy a tirarla. La llevaré de vuelta al gobierno.', 'All right... I will not throw it in. I will take it back to the government.', 'Está bem... Não vou jogar. Vou devolver ao governo.', 'わかった……投げない。政府に返してくる。'],
            ['Eso ya se parece más al hermano que quiero. Ahora espera, podemos ir juntos...', 'That sounds more like the brother I love. Now wait, we can go together...', 'Assim você parece mais com o irmão que eu amo. Agora espere, podemos ir juntos...', 'うん。それでこそ大好きな兄さん。待って、一緒に行こう……'],
        ],
        'outro': [
            ['Cuando terminó nuestra batalla, creo que entendió que lo que hacía no era correcto. Por un momento estuvo muy cerca de lo que nuestro padre quería enseñarle.', 'When our battle ended, I think he understood that what he was doing was wrong. For a moment he came very close to the lesson our father wanted him to learn.', 'Quando nossa batalha terminou, acho que ele entendeu que estava errado. Por um momento, chegou bem perto da lição que nosso pai queria ensinar.', '戦いが終わるころ、兄は自分の間違いに気づいたようでした。一瞬だけ、父が教えたかったことに近づいたんです。'],
            ['Entonces la radio anunció que el gobierno lo consideraba una amenaza. Y él iba precisamente hacia su base para devolverles la roca. Tenía que detenerlo antes de que todo empeorara.', 'Then the radio announced that the government considered him a threat. And he was heading straight to their base to return the rock. I had to stop him before it got worse.', 'Então o rádio anunciou que o governo o considerava uma ameaça. E ele estava indo justamente à base devolver a pedra. Eu precisava detê-lo antes que tudo piorasse.', 'でも無線は、政府が兄を脅威と見なしたと告げました。その基地へ石を返しに行くなんて。手遅れになる前に止めなくちゃ。'],
            ['Salí tras él en mi Night Skull. Los del gobierno vieron venir otra locomotora y decidieron que yo también quería atacarlos. ¡Qué facilidad para sacar conclusiones!', 'I followed him in my Night Skull. The government saw another locomotive approaching and decided I wanted to attack them too. What a gift for jumping to conclusions!', 'Saí atrás dele na minha Night Skull. O governo viu outra locomotiva chegando e decidiu que eu também queria atacar. Que talento para tirar conclusões!', 'ナイトスカルで追いかけたら、政府はもう一台の機関車も攻撃だと思ったようです。早とちりの才能だけはすごいですね。'],
        ],
    },
    {
        'title': ['Solo vengo por mi hermano', 'I am only here for my brother', 'Só vim buscar meu irmão', '兄を迎えに来ただけ'],
        'intro': [
            ['La base del gobierno estaba llena de alarmas, soldados y robots. Yo solo quería encontrar a mi hermano y asegurarme de que no hiciera otra tontería.', 'The government base was full of alarms, soldiers and robots. I only wanted to find my brother and make sure he did not do anything else foolish.', 'A base do governo estava cheia de alarmes, soldados e robôs. Eu só queria achar meu irmão e garantir que ele não fizesse outra besteira.', '政府の基地は警報と兵士とロボットだらけ。私は兄を見つけて、これ以上ばかなことをしないようにしたいだけなのに。'],
            ['Intenté explicarlo, pero un tal Golem me cortó el paso. Parece que aquí las visitas familiares también requieren una batalla.', 'I tried to explain, but someone called Golem blocked my way. Apparently family visits here require a battle too.', 'Tentei explicar, mas um tal de Golem bloqueou meu caminho. Parece que até visita de família exige uma batalha por aqui.', '説明しようとしたら、ゴーレムという相手に道をふさがれました。ここでは家族のお迎えにも戦闘が必要らしいです。'],
        ],
        'before': [
            ['¡Alto! ¡Su locomotora está entrando en una zona restringida!', 'Halt! Your locomotive is entering a restricted area!', 'Alto! Sua locomotiva está entrando em uma área restrita!', '停止せよ！制限区域への侵入を確認！'],
            ['Solo busco a mi hermano. Lleva una roca verde y toma decisiones terribles cuando está enfadado.', 'I am only looking for my brother. He has a green rock and makes terrible decisions when he is angry.', 'Só procuro meu irmão. Ele carrega uma pedra verde e toma decisões horríveis quando está bravo.', '兄を探しているだけです。緑の石を持っていて、怒るとひどい判断をするんです。'],
            ['Descripción compatible con amenaza máxima. ¡Activando protocolo de combate!', 'Description matches a maximum threat. Activating combat protocol!', 'Descrição compatível com ameaça máxima. Ativando protocolo de combate!', '最高度の脅威と一致。戦闘手順を開始！'],
        ],
        'after': [
            ['¿Podemos dejar de disparar y hablar como personas?', 'Can we stop shooting and talk like people?', 'Podemos parar de atirar e conversar como pessoas?', '撃つのをやめて、人みたいに話せませんか？'],
            ['Protocolo de conversación... no disponible.', 'Conversation protocol... unavailable.', 'Protocolo de conversa... indisponível.', '会話手順……利用不可。'],
        ],
        'outro': [
            ['La verdad, no entiendo por qué esos robots hablan como personas. Es muy raro. Después de la pelea ya no encontraba a mi hermano por ninguna parte.', 'Honestly, I do not understand why those robots talk like people. It is very strange. After the fight I could not find my brother anywhere.', 'Sinceramente, não entendo por que esses robôs falam como pessoas. É muito estranho. Depois da luta, não achei meu irmão em lugar nenhum.', 'あのロボットたちが人みたいに話す理由は、今もわかりません。変なの。戦いの後には、兄の姿も見失っていました。'],
            ['Hasta que escuché por radio que había tirado la piedra en un ducto de ventilación. La base iba a estallar. ¡Justo lo que uno espera de una devolución!', 'Then I heard on the radio that he had dropped the rock into a ventilation shaft. The base was going to explode. Just what you expect from a return!', 'Até ouvir no rádio que ele jogou a pedra em um duto de ventilação. A base ia explodir. Exatamente o que se espera de uma devolução!', 'すると無線で、兄が石を換気ダクトへ投げたと聞きました。基地が爆発するそうです。返品って、そんなに大変でしたっけ。'],
            ['Me dio tiempo de salir. A él no. Seguro se quedó viendo la explosión desde dentro... como si fuera el mejor asiento para un espectáculo.', 'I had time to get out. He did not. He probably stayed to watch the explosion from inside... as if that were the best seat in the house.', 'Deu tempo de eu sair. Ele não. Deve ter ficado vendo a explosão por dentro... como se fosse o melhor lugar para assistir.', '私は間に合いました。兄は違いました。きっと中から爆発を眺めていたんでしょう。特等席だとでも思ったのかな。'],
            ['Con todo lo malo que había hecho, no podía confiar en que llegara con nuestro padre. Así que tuve que ir a buscarlo al infierno. Ser hermana menor debería incluir vacaciones.', 'After all the bad things he had done, I could not trust that he would reach our father. So I had to look for him in hell. Being a little sister should come with holidays.', 'Depois de tudo de ruim que fez, eu não podia confiar que chegaria ao nosso pai. Então fui buscá-lo no inferno. Ser irmã caçula devia dar direito a férias.', '悪さを重ねた兄が、父のもとへ行けるとは思えません。それで地獄まで迎えに行くことに。妹業にも休暇がほしいです。'],
        ],
    },
    {
        'title': ['El precio de una comida', 'The price of a meal', 'O preço de uma refeição', '食事一回のお値段'],
        'intro': [
            ['Nunca pensé que ir al infierno sería parte de cuidar a mi hermano. Aunque, repasando su historial, quizá debí haberlo previsto.', 'I never thought going to hell would be part of looking after my brother. Though, looking at his record, perhaps I should have expected it.', 'Nunca pensei que ir ao inferno faria parte de cuidar do meu irmão. Mas, pensando no histórico dele, talvez eu devesse ter previsto.', '兄の世話に地獄行きまで含まれるとは。でも今までの行いを思えば、予想すべきだったのかもしれません。'],
            ['Buscaba a alguien con un gran pico, muchas plumas y unas ganas enormes de no trabajar. Una diablita llamada Onini parecía saber por dónde empezar.', 'I was looking for someone with a huge beak, lots of feathers and an immense desire not to work. A little devil called Onini seemed to know where to start.', 'Eu procurava alguém de bico enorme, muitas penas e uma vontade imensa de não trabalhar. Uma diabinha chamada Onini parecia saber por onde começar.', '大きなくちばしに羽飾り、働きたくない気持ちだけは人一倍。そんな兄を、オニニという悪魔の子が知っていそうでした。'],
        ],
        'before': [
            ['¿Vienes por el pájaro? Aquí las salidas no son gratis.', 'Here for the bird? Exits are not free around here.', 'Veio buscar o passarinho? Aqui a saída não é de graça.', '鳥を迎えに来たの？ここの出口は無料じゃないよ。'],
            ['Es mi hermano. ¿Podrías decirme dónde está?', 'He is my brother. Could you tell me where he is?', 'É meu irmão. Pode me dizer onde ele está?', '兄なんです。どこにいるか教えてくれませんか？'],
            ['Primero demuestra que puedes seguirme el ritmo. ¡Y luego hablamos del menú!', 'First show me you can keep up. Then we can discuss the menu!', 'Primeiro prove que consegue me acompanhar. Depois falamos do cardápio!', 'まずはついてこられるか見せて。メニューの相談はその後！'],
        ],
        'after': [
            ['Está bien, te mostraré el camino. Pero me debes algo de comer.', 'All right, I will show you the way. But you owe me a meal.', 'Está bem, vou mostrar o caminho. Mas você me deve uma refeição.', 'いいよ、案内する。代わりに何かおごってね。'],
            ['Trato hecho. Esta vez pediré yo: no quiero acabar persiguiendo también tu cena.', 'Deal. I will order this time: I do not want to end up chasing your dinner too.', 'Fechado. Desta vez eu peço: não quero acabar correndo atrás do seu jantar também.', '約束ね。注文は私がするよ。夕食まで追いかけることになったら困るから。'],
        ],
        'outro': [
            ['Onini fue bastante amable al mostrarme el camino. Encontramos a mi hermano inconsciente. Yo creo que es tan flojo que simplemente aprovechó para dormirse.', 'Onini was quite kind about showing me the way. We found my brother unconscious. I think he is so lazy he simply took the chance to have a nap.', 'Onini foi bem gentil em mostrar o caminho. Encontramos meu irmão inconsciente. Acho que ele é tão preguiçoso que só aproveitou para tirar um cochilo.', 'オニニは親切に案内してくれました。兄は意識を失っていましたが、あまりに面倒くさくて昼寝しただけのようにも見えます。'],
            ['Me dijo que, si la invitaba a comer, podía llevármelo. Al parecer, a nadie le importaba demasiado que el hijo de un dios volviera a salirse con la suya.', 'She said I could take him if I bought her a meal. Apparently nobody cared much if the son of a god got his own way again.', 'Ela disse que eu podia levá-lo se pagasse uma refeição. Parece que ninguém ligava muito se o filho de um deus escapasse de novo.', '食事をおごれば連れていっていいそうです。神の息子がまた都合よく抜け出しても、誰もあまり気にしないらしくて。'],
            ['Comimos juntas mientras él seguía sin moverse. Una negociación extrañamente agradable. La diablita tiene muy buen apetito; mi hermano, muy buen sueño.', 'We ate together while he still refused to move. A strangely pleasant negotiation. The little devil has a fine appetite; my brother, a fine ability to sleep.', 'Comemos juntas enquanto ele continuava imóvel. Uma negociação estranhamente agradável. A diabinha tem um ótimo apetite; meu irmão, um ótimo sono.', '兄が動かない間に、二人で食事をしました。不思議と楽しい交渉でした。悪魔の子はよく食べて、兄はよく眠ります。'],
            ['Después de pagar, tocaba llevarlo al cielo y explicarle a nuestro padre que esta vez, de verdad, había intentado hacer lo correcto.', 'After paying, it was time to take him to heaven and explain to our father that this time he really had tried to do the right thing.', 'Depois de pagar, era hora de levá-lo ao céu e explicar ao nosso pai que, desta vez, ele realmente tentou fazer o certo.', '会計を済ませたら、兄を天へ。今度こそ正しいことをしようとしたのだと、父に説明しなくちゃ。'],
        ],
    },
    {
        'title': ['Mi propio camino', 'My own path', 'Meu próprio caminho', '私だけの道'],
        'intro': [
            ['Subir al cielo con un hermano dormido no es fácil. Mucho menos cuando la entrada está vigilada por un ángel con demasiados ojos para mirar hacia otro lado.', 'Getting to heaven with a sleeping brother is not easy. Especially when the entrance is guarded by an angel with far too many eyes to look the other way.', 'Subir ao céu com um irmão dormindo não é fácil. Ainda mais com a entrada vigiada por um anjo com olhos demais para olhar para o outro lado.', '眠った兄を天まで運ぶのは大変です。入口の天使は、見逃すには目が多すぎる相手でした。'],
            ['Solo faltaba una última explicación. Bueno, conociendo nuestro viaje, seguramente una última batalla antes de la explicación.', 'All that remained was one last explanation. Well, given our journey, probably one last battle before the explanation.', 'Só faltava uma última explicação. Bem, conhecendo nossa viagem, provavelmente uma última batalha antes da explicação.', 'あとは最後の説明だけ。いえ、この旅の流れなら、説明の前に最後の戦いもありそうです。'],
        ],
        'before': [
            ['No puedes traer al cielo a quien acaba de salir del infierno.', 'You cannot bring someone straight from hell into heaven.', 'Não pode trazer ao céu alguém que acabou de sair do inferno.', '地獄から出たばかりの者を、天へ入れるわけにはいかない。'],
            ['Solo quiero hablar con nuestro padre. Mi hermano entendió algo importante, aunque eligió un momento terrible para demostrarlo.', 'I only want to speak to our father. My brother learned something important, though he picked a terrible moment to show it.', 'Só quero falar com nosso pai. Meu irmão aprendeu algo importante, embora tenha escolhido uma péssima hora para mostrar.', '父と話したいだけです。兄は大切なことを学びました。証明するタイミングは最悪だったけれど。'],
            ['Entonces demuestra que puedes llevar esa carga hasta el final.', 'Then prove that you can carry that burden to the end.', 'Então prove que consegue levar esse peso até o fim.', 'ならば、その重荷を最後まで運べると示せ。'],
        ],
        'after': [
            ['Puedes pasar. Tu padre escuchará lo que tengas que decir.', 'You may pass. Your father will hear what you have to say.', 'Pode passar. Seu pai ouvirá o que tem a dizer.', '通るがよい。父はお前の言葉を聞くだろう。'],
            ['Gracias. Y si vuelve a dormirse durante la conversación, por favor, no lo devuelvan al infierno.', 'Thank you. And if he falls asleep during the conversation, please do not send him back to hell.', 'Obrigada. E, se ele dormir durante a conversa, por favor não o mandem de volta ao inferno.', 'ありがとう。話の途中でまた眠っても、地獄へ送り返さないでくださいね。'],
        ],
        'outro': [
            ['Al final le expliqué a nuestro padre que mi hermano había entendido, en el último minuto, que no se trataba de perseguir la fama de un héroe.', 'At last I explained to our father that, at the very last minute, my brother had understood it was not about chasing a hero\'s fame.', 'No fim, expliquei ao nosso pai que meu irmão entendeu, no último minuto, que não se tratava de perseguir a fama de um herói.', 'ようやく父に説明できました。兄は最後の最後に、英雄の名声を追うことが大事なのではないと気づいたんです。'],
            ['Además, al parecer había detenido algo horrible que estaban haciendo los humanos. Su método necesitaba trabajo, pero quizá eso también contaba para convertirse en un dios nuevo.', 'Besides, he apparently stopped something terrible the humans were doing. His methods needed work, but perhaps that counted toward becoming a new god too.', 'Além disso, parece que impediu algo horrível que os humanos estavam fazendo. O método precisava melhorar, mas talvez isso também contasse para virar um novo deus.', '人間たちの恐ろしい計画も止めたらしいのです。やり方には問題があっても、新しい神になる理由にはなるかもしれません。'],
            ['Nuestro padre lo dejó quedarse. Por fin mi hermano tenía un lugar a su lado. Yo ya estaba pensando en descansar cuando escuché: «Tú todavía tienes una misión en la Tierra».', 'Our father let him stay. At last my brother had a place beside him. I was already thinking about resting when I heard: "You still have a mission on Earth."', 'Nosso pai deixou ele ficar. Enfim meu irmão tinha um lugar ao seu lado. Eu já pensava em descansar quando ouvi: "Você ainda tem uma missão na Terra."', '父は兄が残ることを許しました。やっと父の隣に居場所ができたんです。休もうと思った私には、「お前にはまだ地上での任務がある」と。'],
            ['Había que demostrarle a Thulu que SU hija podía tener un restaurante mejor que el de la hija de aquel dios cósmico. La competencia entre padres debe ser una fuerza de la naturaleza.', 'We had to show Thulu that HIS daughter could run a better restaurant than that cosmic god\'s daughter. Rivalry between fathers must be a force of nature.', 'Era preciso mostrar a Thulu que a filha DELE podia ter um restaurante melhor que o da filha daquele deus cósmico. Rivalidade entre pais deve ser uma força da natureza.', 'あの宇宙の神トゥルーより、自分の娘の店のほうがいいと証明したいそうです。父親同士の競争心も、自然の力なのでしょうね。'],
            ['Y así regresé a mi volcán, esta vez con un puesto de helados llamado Carito. No sé quién eligió el nombre, pero espero que no asuste a los clientes.', 'And so I returned to my volcano, this time with an ice cream stand called Carito. I do not know who chose the name, but I hope it does not scare off the customers.', 'E assim voltei ao meu vulcão, desta vez com uma sorveteria chamada Carito. Não sei quem escolheu o nome, mas espero que não assuste os clientes.', 'こうして火山へ戻り、今度は「カリート」というアイス屋さんを開きました。誰が名づけたのか知りませんが、お客さんが逃げないといいな。'],
            ['Disfruto mucho preparar helados. La competencia con la hija de Thulu no me la tomo tan en serio. Si viene, le guardaré una mesa y le recomendaré mi sabor favorito.', 'I really enjoy making ice cream. I do not take the rivalry with Thulu\'s daughter too seriously. If she visits, I will save her a table and recommend my favorite flavor.', 'Gosto muito de preparar sorvetes. Não levo a competição com a filha de Thulu tão a sério. Se vier, guardo uma mesa e recomendo meu sabor favorito.', 'アイスを作るのは楽しいです。トゥルーの娘との競争は、そんなに本気ではありません。遊びに来たら席を取って、おすすめの味を出しましょう。'],
            ['Creo que todo fue un plan de padre para que dejara de preocuparme tanto por mi hermano y encontrara mi propio camino. Por una vez, me gusta bastante el camino que me tocó.', 'I think it was all Father\'s plan to make me stop worrying so much about my brother and find my own path. For once, I quite like the path I was given.', 'Acho que tudo foi um plano do pai para eu parar de me preocupar tanto com meu irmão e encontrar meu próprio caminho. Desta vez, gostei bastante do caminho que me coube.', 'きっと父は、私が兄の心配ばかりするのをやめて、自分の道を見つけてほしかったのでしょう。今度の道は、私もとても気に入っています。'],
        ],
    },
]

rows = []
for chapter, content in enumerate(chapters):
    for section, values in content.items():
        entries = [values] if section == 'title' else values
        for n, translations in enumerate(entries, 1):
            key = f'story.tzmil.{chapter}.{section}' + ('' if section == 'title' else f'.{n}')
            assert len(translations) == 4 and all(translations), key
            rows.append(dict(zip(['key', 'es', 'en', 'pt', 'ja'], [key] + translations)))
assert len(rows) == 55
Path('Tools/tzmil_story_dialogues.json').write_text(json.dumps(rows, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
path = Path('Assets/Localization/strings.tsv')
lines = path.read_text(encoding='utf-8-sig').splitlines()
updates = {row['key']: '\t'.join(row[f].replace('\n', r'\n') for f in ['key', 'es', 'en', 'pt', 'ja']) for row in rows}
for i, line in enumerate(lines):
    key = line.split('\t', 1)[0]
    if key in updates:
        lines[i] = updates.pop(key)
lines.extend(updates.values())
path.write_text('\n'.join(lines) + '\n', encoding='utf-8')
report = ['# Historia de Tzmil', '', 'Arte: muestra principiante aprobada en Tools/tzmil_art_trials/tzmil_style_beginner.png.', 'Ruta: volcán (holoajedrez R-20) → base del gobierno (Golem) → infierno (Onini) → cielo (ángel existente) → gracias.', 'Lectura manual y paleta del hero; sin cuestionarios ni ascenso adicionales.', '']
for c, content in enumerate(chapters):
    report += [f'## {c + 1}. {content["title"][0]}', '']
    for section in ['intro', 'before', 'after', 'outro']:
        report += [f'### {section}', ''] + [t[0] for t in content[section]] + ['']
Path('Tools/tzmil_story.md').write_text('\n\n'.join(report) + '\n', encoding='utf-8')
print(f'{len(rows)} keys × 4 languages = {len(rows)*4} localized entries.')
