import json
from pathlib import Path
rows=[]
def add(k,es,en,pt,ja): rows.append(dict(zip(['key','es','en','pt','ja'],[k,es,en,pt,ja])))
def section(c,side,lines):
 for n,line in enumerate(lines,1): add(f'story.nath.{c}.{side}.{n}',*line)
for c,t in enumerate([
 ['Una chica sencilla','A simple girl','Uma garota simples','普通の女の子'],
 ['Luces bonitas y malas decisiones','Pretty lights, bad decisions','Luzes bonitas, decisões ruins','きれいな光とひどい判断'],
 ['Sin descanso en el cielo','No rest in heaven','Sem descanso no céu','天国でも休みなし'],
 ['La puerta estaba abierta','The door was open','A porta estava aberta','開いていた扉'],
 ['Una revancha para mañana','A rematch tomorrow','Uma revanche para amanhã','明日も再戦'],
]): add(f'story.nath.{c}.title',*t)
add('hero.character.nath','Nath Star','Nath Star','Nath Star','ナス・スター')
section(0,'intro',[
 ['Soy Nath Star. Una chica sencilla, de gustos normales y costumbres tranquilas. No se rían antes de que termine.','I am Nath Star. A simple girl with normal tastes and peaceful habits. Let me finish before you laugh.','Sou Nath Star. Uma garota simples, com gostos normais e hábitos tranquilos. Deixem eu terminar antes de rir.','私はナス・スター。普通の趣味で、穏やかに暮らす普通の女の子。笑うのは最後まで聞いてからね。'],
 ['Desde pequeña hice boxeo, muay thai y carreras de autos monstruo. Así que lo normal era que terminara siendo maquinista. Es una evolución bastante lógica.','Since I was little I did boxing, Muay Thai and monster-truck races. Naturally I became a train driver. A perfectly logical progression.','Desde pequena fiz boxe, muay thai e corridas de monster truck. Então virei maquinista, naturalmente. Uma evolução bem lógica.','小さい頃からボクシング、ムエタイ、モンスタートラックのレース。だから機関士になった。すごく自然な流れでしょ。'],
 ['La verdad es que simplemente me gusta pelear. Mi ReyMar y yo entendemos muy bien eso de avanzar sin pedir permiso.','The truth is I just like fighting. ReyMar and I understand moving forward without asking permission.','A verdade é que eu simplesmente gosto de lutar. Eu e ReyMar entendemos bem essa história de avançar sem pedir licença.','本当は、ただ戦うのが好きなだけ。レイマーも私も、許可なんか待たずに進むのが得意なの。'],
 ['Entonces llegaron unos sujetos del gobierno. Me ofrecieron una batalla legendaria contra alienígenas. Solo tenía que aventar una roca verde en su nave o en su base, algo así.','Then some government agents offered me a legendary battle against aliens. I just had to throw a green rock into their ship or base. Something like that.','Então uns agentes do governo ofereceram uma batalha lendária contra alienígenas. Era só jogar uma pedra verde na nave ou na base deles. Algo assim.','そこへ政府の人たちが来て、宇宙人との伝説の戦いを持ちかけた。緑の石を宇宙船か基地に投げ込めばいいらしい。そんな感じ。'],
 ['No me lo pensé dos veces. ¡Salté a la acción! Y si el camino al viaje espacial empieza en un volcán, pues se empieza en un volcán.','I did not think twice. I jumped straight into action! If the road to space starts in a volcano, then a volcano it is.','Não pensei duas vezes. Pulei para a ação! Se o caminho para o espaço começa num vulcão, então vamos ao vulcão.','迷わず飛び込んだ！宇宙への道が火山から始まるなら、火山から始めればいい！'],
])
section(0,'before',[
 ['No sé qué te prometieron, pero esta zona está bajo mi cuidado.','I do not know what they promised you, but this area is under my protection.','Não sei o que prometeram, mas esta área está sob meus cuidados.','何を約束されたか知らないけど、ここは私が守っているの。'],
 ['¿Una hija de dioses? Perfecto. Ya me estaba preocupando que la primera pelea fuera aburrida.','A daughter of gods? Perfect. I was worried the first fight would be boring.','Uma filha de deuses? Perfeito. Já estava com medo de a primeira luta ser chata.','神様の娘？最高。最初の戦いが退屈だったらどうしようかと思ってた。'],
 ['Con esa actitud vas a conseguir muchas peleas y muy pocas respuestas.','With that attitude you will get plenty of fights and very few answers.','Com essa atitude vai conseguir muitas lutas e poucas respostas.','その態度なら、答えより戦いの方がたくさん見つかるわね。'],
])
section(0,'after',[
 ['¡Ni siquiera los hijos de dioses pueden con mi grandeza!','Even the children of gods cannot handle my greatness!','Nem os filhos de deuses aguentam minha grandeza!','神様の子だって、私の偉大さには勝てない！'],
 ['Solo te pedí que bajaras la velocidad.','I only asked you to slow down.','Eu só pedi para diminuir a velocidade.','速度を落としてって言っただけなんだけど。'],
])
section(0,'outro',[
 ['Ni siquiera los hijos de dioses pudieron pararme. Ahora solo tenía que llegar antes de que alguien me ganara ese viaje espacial.','Even the children of gods could not stop me. Now I only had to arrive before somebody stole my trip to space.','Nem os filhos de deuses me pararam. Agora era só chegar antes que alguém pegasse minha viagem espacial.','神様の子でも止められなかった。あとは誰かに宇宙行きを取られる前に着くだけ。'],
 ['Solo tenía que ir a la estúpida ciudad. Fácil. Aunque primero había una oficial empeñada en hacerme preguntas.','I just had to reach the stupid city. Easy. Except there was an officer determined to ask me questions first.','Era só chegar à cidade idiota. Fácil. Só que primeiro havia uma oficial decidida a fazer perguntas.','あのバカな街に行くだけ。簡単。なのに、その前に質問したがる係員がいた。'],
])
section(1,'intro',[
 ['¿Un cuestionario para comprobar que soy humana? Está bien. También puedo derrotar cinco preguntas. Después voy directa al lanzador.','A questionnaire to prove I am human? Fine. I can defeat five questions too. Then straight to the launcher.','Um questionário para provar que sou humana? Tudo bem. Também consigo derrotar cinco perguntas. Depois vou direto ao lançador.','人間か確かめる質問？いいわ。五問くらい倒してあげる。それが済んだら発射台へ一直線よ。'],
])
section(1,'before',[
 ['El último lanzamiento no va a esperar a nadie.','The last launch will not wait for anyone.','O último lançamento não vai esperar ninguém.','最後の打ち上げは誰も待たないぞ。'],
 ['¡Pues será mejor que todos se aparten de mi ReyMar!','Then everyone had better get out of ReyMar’s way!','Então é melhor todos saírem da frente da minha ReyMar!','じゃあ、全員レイマーの前からどいて！'],
])
section(1,'after',[
 ['¡Eso también cuenta como ganar una pelea! ¡Al lanzador!','That counts as winning a fight too! To the launcher!','Isso também conta como vencer uma luta! Ao lançador!','これも勝ったってことでしょ！発射台へ！'],
])
section(1,'outro',[
 ['No entiendo qué pasó. Entré en el lanzador, pisé a fondo y de repente vi unas luces muy bonitas. Hasta ahí, todo excelente.','I do not understand what happened. I entered the launcher, floored it and suddenly saw lovely lights. So far, excellent.','Não entendo o que aconteceu. Entrei no lançador, acelerei tudo e vi umas luzes lindas. Até aí, excelente.','何が起きたのかわからない。発射台に入って全開にしたら、きれいな光が見えた。そこまでは最高だった。'],
 ['Pero ahora estaba en un mundo esponjoso lleno de ángeles. Yo pedí alienígenas. Esto tenía demasiadas plumas.','But now I was in a fluffy world full of angels. I asked for aliens. This place had far too many feathers.','Mas agora estava num mundo fofinho cheio de anjos. Eu pedi alienígenas. Havia penas demais por aqui.','でも着いたのは、天使だらけのふわふわした世界。頼んだのは宇宙人よ。ここは羽が多すぎる。'],
 ['¡Estoy segura de que esos tontos del gobierno cometieron un error! ¡Y esto no se va a quedar así!','Those government idiots definitely made a mistake! And they are not getting away with it!','Tenho certeza de que os idiotas do governo erraram! E isso não vai ficar assim!','政府のバカどもが絶対に間違えた！このままじゃ済まさない！'],
])
section(2,'intro',[
 ['Si esto era el cielo, alguien tendría que explicarme cómo salir. Y si su mejor guerrera prefería pelear, pues tampoco iba a quejarme.','If this was heaven, someone had to tell me how to leave. If their best warrior preferred fighting, I would not complain.','Se isto era o céu, alguém teria que me explicar como sair. Se a melhor guerreira preferisse lutar, não ia reclamar.','ここが天国なら、誰か出口を教えなさいよ。最強の戦士が戦いたいなら、それでも大歓迎だけど。'],
])
section(2,'before',[
 ['¡Otra más! ¿Nadie sabe leer el horario de visitas?','Another one! Can nobody read the visiting hours?','Outra! Ninguém sabe ler o horário de visitas?','また来た！誰も面会時間を読めないの？'],
 ['No vine de visita. Vine por una batalla legendaria y a exigir una explicación. En ese orden.','I am not visiting. I came for a legendary battle and an explanation. In that order.','Não vim visitar. Vim por uma batalha lendária e uma explicação. Nessa ordem.','面会じゃない。伝説の戦いと説明を求めに来た。その順番でね。'],
 ['Me debían un día libre. Ahora me deben dos.','They owed me a day off. Now they owe me two.','Me deviam um dia de folga. Agora devem dois.','一日休みをもらうはずだったのに。これで二日分ね。'],
])
section(2,'after',[
 ['¿Eso era lo mejor del cielo? ¿Hay otra fila para las peleas legendarias?','Was that heaven’s best? Is there another queue for legendary fights?','Era o melhor do céu? Tem outra fila para lutas lendárias?','これが天国の最強？伝説の戦いは別の列なの？'],
 ['Sí. Al fondo. Muy, muy al fondo.','Yes. Down there. Very, very far down.','Sim. Lá embaixo. Muito, muito embaixo.','あるわ。下の方。ずっと、ずーっと下。'],
])
section(2,'outro',[
 ['Después de ganarle a sus mejores guerreros, los del cielo aceptaron darme otra batalla legendaria. Qué atentos.','After I beat their best warriors, heaven agreed to give me another legendary battle. How considerate.','Depois de vencer seus melhores guerreiros, o céu aceitou me dar outra batalha lendária. Que gentileza.','最強の戦士たちに勝ったら、天国は次の伝説の戦いを用意してくれた。親切ね。'],
 ['Aunque sonaba a que solo querían deshacerse de mí. Abrieron un camino y me mandaron derechito al infierno. Por fin un lugar con ganas de pelear.','Though it sounded like they just wanted rid of me. They opened a path straight to hell. Finally, somewhere ready to fight.','Mas parecia que só queriam se livrar de mim. Abriram um caminho direto ao inferno. Finalmente um lugar disposto a lutar.','どうも追い払いたかっただけみたい。開いた道は地獄に直通。ようやく戦う気のある場所に来たわ。'],
])
section(3,'intro',[
 ['El infierno tenía comida, trenes y gente dispuesta a discutir. Bastante prometedor. La diablita de pelo blanco parecía conocer todas las salidas.','Hell had food, trains and people ready to argue. Promising. The white-haired demon girl seemed to know every exit.','O inferno tinha comida, trens e gente pronta para discutir. Promissor. A diabinha de cabelo branco parecia conhecer todas as saídas.','地獄には食べ物と列車と、喧嘩っ早い連中がいた。有望ね。白髪の悪魔の子は出口を全部知っていそうだった。'],
])
section(3,'before',[
 ['¿Tú también vienes a escapar o solo a arruinarme la comida?','Are you escaping too, or just ruining my meal?','Também veio fugir ou só estragar minha comida?','あんたも脱走？それとも食事の邪魔をしに来たの？'],
 ['Primero peleamos. Después podemos hablar del menú.','First we fight. Then we can discuss the menu.','Primeiro lutamos. Depois falamos do cardápio.','まず戦う。メニューの話はその後ね。'],
 ['Como enfríes mi cena, esta pelea sí va a ser personal.','If my dinner gets cold, this fight gets personal.','Se meu jantar esfriar, essa luta vai ser pessoal.','夕飯が冷めたら、今度こそ本気で恨むからね。'],
])
section(3,'after',[
 ['No eres mala, diablita. Otro día repetimos.','Not bad, demon girl. We should do this again.','Nada mal, diabinha. Outro dia repetimos.','やるじゃない、悪魔ちゃん。またやりましょ。'],
 ['Otro día que no tenga hambre. O sea, nunca.','Another day when I am not hungry. So, never.','Outro dia em que eu não tenha fome. Ou seja, nunca.','お腹が空いてない日にね。つまり、一生ないわ。'],
])
section(3,'outro',[
 ['La diablita no era mala, pero yo ya estaba cansándome de todo esto. Todavía tenía que vengarme del gobierno por su error.','The demon girl was not bad, but I was getting tired of all this. I still had to get revenge on the government for its mistake.','A diabinha não era má, mas eu já estava cansando de tudo isso. Ainda precisava me vingar do governo pelo erro.','悪魔ちゃんは悪くなかったけど、そろそろうんざりしてきた。政府の間違いにも仕返ししないと。'],
 ['Creo que los demonios también se cansaron de luchar conmigo a todas horas. Simplemente dejaron abierta la puerta de regreso. Qué descuido tan conveniente.','I think the demons got tired of fighting me all day too. They simply left the way home open. A very convenient oversight.','Acho que os demônios cansaram de lutar comigo toda hora. Simplesmente deixaram a porta de volta aberta. Que descuido conveniente.','悪魔たちも四六時中の戦いに疲れたらしい。帰りの扉を開けっぱなしにしてくれた。ずいぶん都合のいい不注意ね。'],
 ['Lamentablemente, cuando llegué a la base por mi venganza, ya no quedaba nadie. Ni un guardia para calentar. Una falta de respeto.','Unfortunately, by the time I reached the base for revenge, nobody was left. Not even a guard to warm up against. Rude.','Infelizmente, quando cheguei à base para a vingança, não havia ninguém. Nem um guarda para aquecer. Uma falta de respeito.','でも仕返しに基地へ着いた時には誰もいなかった。準備運動の相手になる警備員すらいない。失礼しちゃう。'],
 ['Vi a BBA, pero como siempre solo balbuceaba cosas. Creo que estaba triste o algo. Yo tampoco encontré a quién reclamarle.','I saw BBA, but she just babbled as usual. I think she was sad or something. I could not find anyone to complain to either.','Vi BBA, mas ela só balbuciava como sempre. Acho que estava triste ou algo assim. Também não achei a quem reclamar.','ＢＢＡはいたけど、いつも通り何かぶつぶつ言っていた。悲しんでいたのかも。私も文句を言う相手を見つけられなかった。'],
 ['Entonces escuché por radio una invitación a un maid café. Odio admitirlo, pero me encantan esas cosas. ¡Y Tzmil había abierto un puesto de helados! Ya quería comprar uno y retarlas a un duelo.','Then the radio mentioned a maid café. I hate admitting it, but I love those places. And Tzmil had opened an ice-cream stand! I wanted a cone and a rematch.','Então ouvi no rádio um convite para um maid café. Odeio admitir, mas adoro essas coisas. E Tzmil abriu uma sorveteria! Queria um sorvete e uma revanche.','その時、ラジオからメイド喫茶の招待が聞こえた。認めたくないけど、大好きなの。しかもツミルがアイス屋を開いたって！買って再戦を申し込まないと。'],
 ['Pero por el camino encontré algo mucho más interesante en el bosque. Un tren que parecía disfrutar las peleas tanto como yo.','But on the way I found something much more interesting in the woods. A train that seemed to enjoy fighting as much as I did.','Mas no caminho encontrei algo muito mais interessante no bosque. Um trem que parecia gostar de lutar tanto quanto eu.','でも途中の森で、もっと面白いものを見つけた。私と同じくらい戦いを楽しみそうな列車をね。'],
])
section(4,'intro',[
 ['Un tipo blanco, alto y sin cara estaba junto a una locomotora que casi sonreía al verme. No iba a dejar pasar esa invitación.','A tall white faceless fellow stood by a locomotive that almost smiled at me. I was not passing up that invitation.','Um sujeito branco, alto e sem rosto estava ao lado de uma locomotiva que quase sorriu para mim. Não ia recusar o convite.','背が高くて白くて顔のない男。その隣の機関車は、私を見て笑った気がした。この誘いは断れない。'],
 ['¡Eh, rarito! No sé qué dibujas, pero tu máquina y mi ReyMar tienen una conversación pendiente.','Hey, weirdo! No idea what you are drawing, but your engine and my ReyMar have something to discuss.','Ei, esquisito! Não sei o que desenha, mas sua máquina e minha ReyMar têm uma conversa pendente.','ねえ、変な人！何を描いてるか知らないけど、あんたの機関車とレイマーには話があるの。'],
])
section(4,'before',[
 ['Solo quiero dibujar tranquilo. Aunque T.exe parece bastante interesada en tu propuesta.','I just want to draw in peace. Though T.exe seems interested in your proposal.','Só quero desenhar em paz. Mas T.exe parece interessada na proposta.','静かに絵を描きたいだけなんだ。でもＴ.exeは君の提案に興味があるみたいだね。'],
 ['¡Lo sabía! Esa máquina tiene la batalla por dentro.','I knew it! That engine has battle in her soul.','Eu sabia! Essa máquina tem a batalha na alma.','やっぱり！あの機関車、心の中から戦いたがってる。'],
 ['Mientras no rompan mis dibujos, adelante.','As long as you do not break my drawings, go ahead.','Desde que não estraguem meus desenhos, podem ir.','絵を壊さないなら、どうぞ。'],
])
section(4,'after',[
 ['¡Por fin alguien que lo hace interesante! Mañana repetimos.','Finally, someone who makes it interesting! Again tomorrow.','Finalmente alguém que deixa interessante! Amanhã repetimos.','ようやく面白い相手が見つかった！明日もやるわよ。'],
 ['Creo que a ella también le gustaría. Yo voy a necesitar más papel.','I think she would like that too. I will need more paper.','Acho que ela também gostaria. Vou precisar de mais papel.','彼女もそうしたいみたいだ。僕は紙を増やしておくよ。'],
])
section(4,'outro',[
 ['El chico era realmente bueno. Y podía sentir que su máquina amaba competir. No tenía que explicarle nada: la batalla estaba dentro de ella.','The guy was really good. And I could feel how much his engine loved competing. No explanation needed: battle was in her soul.','O rapaz era muito bom. E eu sentia que sua máquina amava competir. Não precisava explicar: a batalha estava dentro dela.','あいつは本当に強かった。機関車も競うのが大好きだと伝わってきた。説明なんていらない。戦いが心に宿っていた。'],
 ['Desde entonces, cada vez que su locomotora salía del trabajo, yo la retaba a una batalla. ¡Parecía disfrutarlo tanto como yo!','From then on, every time his locomotive finished work, I challenged her. She seemed to enjoy it as much as I did!','Desde então, sempre que a locomotiva saía do trabalho, eu a desafiava. Parecia se divertir tanto quanto eu!','それからは機関車の仕事が終わるたびに勝負を挑んだ。向こうも私と同じくらい楽しんでいたわ！'],
 ['A Faceless, que así se llama el rarito, le encantaba dibujarnos. No lo culpo. ¡Soy toda una preciosura!','Faceless, that is the weirdo’s name, loved drawing us. Cannot blame him. I am gorgeous!','Faceless, esse é o nome do esquisito, adorava nos desenhar. Não o culpo. Sou uma preciosidade!','変な人の名前はフェイスレス。私たちを描くのが大好きらしい。無理もないわ。私はこんなに美人だもの！'],
 ['No obtuve mi venganza contra el gobierno. Pero encontré a alguien con quien luchar todos los días, y eso terminó importándome bastante más.','I never got revenge on the government. But I found someone to fight every day, and that ended up mattering much more.','Não me vinguei do governo. Mas achei alguém com quem lutar todos os dias, e isso acabou importando muito mais.','政府への仕返しはできなかった。でも毎日戦える相手が見つかった。結局、そっちの方がずっと大事になったの。'],
 ['Lo mejor es que siempre lo hace interesante. Bien, ReyMar... mañana toca revancha. Y después podemos ir por ese helado.','Best of all, she always keeps it interesting. Well, ReyMar... rematch tomorrow. Then we can get that ice cream.','O melhor é que ela sempre deixa interessante. Bem, ReyMar... amanhã tem revanche. Depois podemos tomar aquele sorvete.','何より、いつも面白い勝負になる。さあレイマー、明日も再戦よ。その後は例のアイスを食べに行こう。'],
])
assert len({r['key'] for r in rows})==len(rows)
for r in rows: assert all(r.values()) and all('\n' not in v and '\t' not in v for v in r.values())
Path('Tools/nath_story_dialogues.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
p=Path('Assets/Localization/strings.tsv');lines=p.read_text(encoding='utf-8-sig').splitlines();updates={r['key']:'\t'.join(r[k] for k in ['key','es','en','pt','ja']) for r in rows}
for i,line in enumerate(lines):
 key=line.split('\t',1)[0]
 if key in updates: lines[i]=updates.pop(key)
lines.extend(updates.values());p.write_text('\n'.join(lines)+'\n',encoding='utf-8')
Path('Tools/nath_story.md').write_text('# Nath Star / ReyMar\n\nVolcán (Tzmil) → cuestionario humano → ciudad/carrera → cielo (Gabriel/Gora-P) → infierno (Onini) → bosque/selva (Faceless/T.exe) → Gracias.\n\nArte aficionado de trazos simples y caras exageradas de combate. Nath siempre morena, con cara y cuerpo del mismo tono, uniforme morado y ojos verdes.\n\n'+'\n\n'.join(r['key']+'\n'+r['es'] for r in rows)+'\n',encoding='utf-8')
print(f'{len(rows)} keys in four languages')
