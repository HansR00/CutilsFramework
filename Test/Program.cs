﻿using System;

namespace Test
{
    class Test
    {
        public static void Main()
        {
            //int count = 0;
            //DateTime epochDate = new DateTime( 1970, 1, 1, 0, 0, 0, 0 );

            //long firstDate = javascriptValues[ 0 ];

            //foreach ( long thisDate in javascriptValues )
            //{

            //    if ( thisDate < firstDate ) Console.WriteLine( $"Thisdate is not sorted in list: {count}:{epochDate.AddMilliseconds( thisDate )}" );
            //    count++;
            //}

            //Console.WriteLine( $"Done - press any key..." );
            //Console.ReadKey();

            int[] Frequencies = { 1, 2, 3, 4, 5, 6, 10, 12, 15, 20, 30, 60 };

            foreach(int a in Frequencies)
            {
                int b = 10;
                Console.WriteLine( $"{b} % {a} = {b % a}" );
            }

            Console.ReadLine();

            return;
        }

        static long[] javascriptValues = {
        1697445300000,
        1697445600000,
        1697445900000,
        1697446200000,
        1697446500000,
        1697446800000,
        1697447100000,
        1697447400000,
        1697447700000,
        1697448000000,
        1697448300000,
        1697448600000,
        1697448900000,
        1697449200000,
        1697449500000,
        1697449800000,
        1697450100000,
        1697450400000,
        1697450700000,
        1697451000000,
        1697451300000,
        1697451600000,
        1697451900000,
        1697452200000,
        1697452500000,
        1697452800000,
        1697453100000,
        1697453400000,
        1697453700000,
        1697454000000,
        1697454300000,
        1697454600000,
        1697454900000,
        1697455200000,
        1697455500000,
        1697455800000,
        1697456100000,
        1697456400000,
        1697456700000,
        1697457000000,
        1697457300000,
        1697457600000,
        1697457900000,
        1697458200000,
        1697458500000,
        1697458800000,
        1697459100000,
        1697459400000,
        1697459700000,
        1697460000000,
        1697460300000,
        1697460600000,
        1697460900000,
        1697461200000,
        1697461500000,
        1697461800000,
        1697462100000,
        1697462400000,
        1697462700000,
        1697463000000,
        1697463300000,
        1697463600000,
        1697463900000,
        1697464200000,
        1697464500000,
        1697464800000,
        1697465100000,
        1697465400000,
        1697465700000,
        1697466000000,
        1697466300000,
        1697466600000,
        1697466900000,
        1697467200000,
        1697467500000,
        1697467800000,
        1697468100000,
        1697468400000,
        1697468700000,
        1697469000000,
        1697469300000,
        1697469600000,
        1697469900000,
        1697470200000,
        1697470500000,
        1697470800000,
        1697471100000,
        1697471400000,
        1697471700000,
        1697472000000,
        1697472300000,
        1697472600000,
        1697472900000,
        1697473200000,
        1697473500000,
        1697473800000,
        1697474100000,
        1697474400000,
        1697474700000,
        1697475000000,
        1697475300000,
        1697475600000,
        1697475900000,
        1697476200000,
        1697476500000,
        1697476800000,
        1697477100000,
        1697477400000,
        1697477700000,
        1697478000000,
        1697478300000,
        1697478600000,
        1697478900000,
        1697479200000,
        1697479500000,
        1697479800000,
        1697480100000,
        1697480400000,
        1697480700000,
        1697481000000,
        1697481300000,
        1697481600000,
        1697481900000,
        1697482200000,
        1697482500000,
        1697482800000,
        1697483100000,
        1697483400000,
        1697483700000,
        1697484000000,
        1697484300000,
        1697484600000,
        1697484900000,
        1697485200000,
        1697485500000,
        1697485800000,
        1697486100000,
        1697486400000,
        1697487000000,
        1697487300000,
        1697487600000,
        1697487900000,
        1697488200000,
        1697488500000,
        1697488800000,
        1697489100000,
        1697489400000,
        1697489700000,
        1697490000000,
        1697490300000,
        1697490600000,
        1697490900000,
        1697491200000,
        1697491500000,
        1697491800000,
        1697492100000,
        1697492400000,
        1697492700000,
        1697493000000,
        1697493300000,
        1697493600000,
        1697493900000,
        1697494200000,
        1697494500000,
        1697494800000,
        1697495100000,
        1697495400000,
        1697495700000,
        1697496000000,
        1697496300000,
        1697496600000,
        1697496900000,
        1697497200000,
        1697497500000,
        1697497800000,
        1697498100000,
        1697498400000,
        1697498700000,
        1697499000000,
        1697499300000,
        1697499600000,
        1697499900000,
        1697500200000,
        1697500500000,
        1697500800000,
        1697501100000,
        1697501400000,
        1697501700000,
        1697502000000,
        1697502300000,
        1697502600000,
        1697502900000,
        1697503200000,
        1697503500000,
        1697503800000,
        1697504100000,
        1697504400000,
        1697504700000,
        1697505000000,
        1697505300000,
        1697505600000,
        1697505900000,
        1697506200000,
        1697506500000,
        1697506800000,
        1697507100000,
        1697507400000,
        1697507700000,
        1697508000000,
        1697508300000,
        1697508600000,
        1697508900000,
        1697509200000,
        1697509500000,
        1697509800000,
        1697510100000,
        1697510400000,
        1697510700000,
        1697511000000,
        1697511300000,
        1697511600000,
        1697511900000,
        1697512200000,
        1697512500000,
        1697512800000,
        1697513100000,
        1697513400000,
        1697513700000,
        1697514000000,
        1697514300000,
        1697514600000,
        1697514900000,
        1697515200000,
        1697515500000,
        1697515800000,
        1697516100000,
        1697516400000,
        1697516700000,
        1697517000000,
        1697517300000,
        1697517600000,
        1697517900000,
        1697518200000,
        1697518500000,
        1697518800000,
        1697519100000,
        1697519400000,
        1697519700000,
        1697520000000,
        1697520300000,
        1697520600000,
        1697520900000,
        1697521200000,
        1697521500000,
        1697521800000,
        1697522100000,
        1697522400000,
        1697522700000,
        1697523000000,
        1697523300000,
        1697523600000,
        1697523900000,
        1697524200000,
        1697524500000,
        1697524800000,
        1697525100000,
        1697525400000,
        1697525700000,
        1697526000000,
        1697526300000,
        1697526600000,
        1697526900000,
        1697527200000,
        1697527500000,
        1697527800000,
        1697528100000,
        1697528400000,
        1697528700000,
        1697529000000,
        1697529300000,
        1697529600000,
        1697529900000,
        1697530200000,
        1697530500000,
        1697530800000,
        1697531100000,
        1697531400000,
        1697531700000,
        1697532000000,
        1697532300000,
        1697532600000,
        1697532900000,
        1697533200000,
        1697533500000,
        1697533800000,
        1697534100000,
        1697534400000,
        1697534700000,
        1697535000000,
        1697535300000,
        1697535600000,
        1697535900000,
        1697536200000,
        1697536500000,
        1697536800000,
        1697537100000,
        1697537400000,
        1697537700000,
        1697538000000,
        1697538300000,
        1697538600000,
        1697538900000,
        1697539200000,
        1697539500000,
        1697539800000,
        1697540100000,
        1697540400000,
        1697540700000,
        1697541000000,
        1697541300000,
        1697541600000,
        1697541900000,
        1697542200000,
        1697542500000,
        1697542800000,
        1697543100000,
        1697543400000,
        1697543700000,
        1697544000000,
        1697544300000,
        1697544600000,
        1697544900000,
        1697545200000,
        1697545500000,
        1697545800000,
        1697546100000,
        1697546400000,
        1697546700000,
        1697547000000,
        1697547300000,
        1697547600000,
        1697547900000,
        1697548200000,
        1697548500000,
        1697548800000,
        1697549100000,
        1697549400000,
        1697549700000,
        1697550000000,
        1697550300000,
        1697550600000,
        1697550900000,
        1697551200000,
        1697551500000,
        1697551800000,
        1697552100000,
        1697552400000,
        1697552700000,
        1697553000000,
        1697553300000,
        1697553600000,
        1697553900000,
        1697554200000,
        1697554500000,
        1697554800000,
        1697555100000,
        1697555400000,
        1697555700000,
        1697556000000,
        1697556300000,
        1697556600000,
        1697556900000,
        1697557200000,
        1697557500000,
        1697557800000,
        1697558100000,
        1697558400000,
        1697558700000,
        1697559000000,
        1697559300000,
        1697559600000,
        1697559900000,
        1697560200000,
        1697560500000,
        1697560800000,
        1697561100000,
        1697561400000,
        1697561700000,
        1697562000000,
        1697562300000,
        1697562600000,
        1697562900000,
        1697563200000,
        1697563500000,
        1697563800000,
        1697564100000,
        1697564400000,
        1697564700000,
        1697565000000,
        1697565300000,
        1697565600000,
        1697565900000,
        1697566200000,
        1697566500000,
        1697566800000,
        1697567100000,
        1697567400000,
        1697567700000,
        1697568000000,
        1697568300000,
        1697568600000,
        1697568900000,
        1697569200000,
        1697569500000,
        1697569800000,
        1697570100000,
        1697570400000,
        1697570700000,
        1697571000000,
        1697571300000,
        1697571600000,
        1697571900000,
        1697572200000,
        1697572500000,
        1697572800000,
        1697573100000,
        1697573400000,
        1697573700000,
        1697574000000,
        1697574300000,
        1697574600000,
        1697574900000,
        1697575200000,
        1697575500000,
        1697575800000,
        1697576100000,
        1697576400000,
        1697576700000,
        1697577000000,
        1697577300000,
        1697577600000,
        1697577900000,
        1697578200000,
        1697578500000,
        1697578800000,
        1697579100000,
        1697579400000,
        1697579700000,
        1697580000000,
        1697580300000,
        1697580600000,
        1697580900000,
        1697581200000,
        1697581500000,
        1697581800000,
        1697582100000,
        1697582400000,
        1697582700000,
        1697583000000,
        1697583300000,
        1697583600000,
        1697583900000,
        1697584200000,
        1697584500000,
        1697584800000,
        1697585100000,
        1697585400000,
        1697585700000,
        1697586000000,
        1697586300000,
        1697586600000,
        1697586900000,
        1697587200000,
        1697587500000,
        1697587800000,
        1697588100000,
        1697588400000,
        1697588700000,
        1697589000000,
        1697589300000,
        1697589600000,
        1697589900000,
        1697590200000,
        1697590500000,
        1697590800000,
        1697591100000,
        1697591400000,
        1697591700000,
        1697592000000,
        1697592300000,
        1697592600000,
        1697592900000,
        1697593200000,
        1697593500000,
        1697593800000,
        1697594100000,
        1697594400000,
        1697594700000,
        1697595000000,
        1697595300000,
        1697595600000,
        1697595900000,
        1697596200000,
        1697596500000,
        1697596800000,
        1697597100000,
        1697597400000,
        1697597700000,
        1697598000000,
        1697598300000,
        1697598600000,
        1697598900000,
        1697599200000,
        1697599500000,
        1697599800000,
        1697600100000,
        1697600400000,
        1697600700000,
        1697601000000,
        1697601300000,
        1697601600000,
        1697601900000,
        1697602200000,
        1697602500000,
        1697602800000,
        1697603100000,
        1697603400000,
        1697603700000,
        1697604000000,
        1697604300000,
        1697604600000,
        1697604900000,
        1697605200000,
        1697605500000,
        1697605800000,
        1697606100000,
        1697606400000,
        1697606700000,
        1697607000000,
        1697607300000,
        1697607600000,
        1697607900000,
        1697608200000,
        1697608500000,
        1697608800000,
        1697609100000,
        1697609400000,
        1697609700000,
        1697610000000,
        1697610300000,
        1697610600000,
        1697610900000,
        1697611200000,
        1697611500000,
        1697611800000,
        1697612100000,
        1697612400000,
        1697612700000,
        1697613000000,
        1697613300000,
        1697613600000,
        1697613900000,
        1697614200000,
        1697614500000,
        1697614800000,
        1697615100000,
        1697615400000,
        1697615700000,
        1697616000000,
        1697616300000,
        1697616600000,
        1697616900000,
        1697617200000,
        1697617500000,
        1697617800000,
        1697618100000,
        1697618400000,
        1697618700000,
        1697619000000,
        1697619300000,
        1697619600000,
        1697619900000,
        1697620200000,
        1697620500000,
        1697620800000,
        1697621100000,
        1697621400000,
        1697621700000,
        1697622000000,
        1697622300000,
        1697622600000,
        1697622900000,
        1697623200000,
        1697623500000,
        1697623800000,
        1697624100000,
        1697624400000,
        1697624700000,
        1697625000000,
        1697625300000,
        1697625600000,
        1697625900000,
        1697626200000,
        1697626500000,
        1697626800000,
        1697627100000,
        1697627400000,
        1697627700000,
        1697628000000,
        1697628300000,
        1697628600000,
        1697628900000,
        1697629200000,
        1697629500000,
        1697629800000,
        1697630100000,
        1697630400000,
        1697630700000,
        1697631000000,
        1697631300000,
        1697631600000,
        1697631900000,
        1697632200000,
        1697632500000,
        1697632800000,
        1697633100000,
        1697633700000,
        1697634000000,
        1697634300000,
        1697634600000,
        1697634900000,
        1697635200000,
        1697635500000,
        1697635800000,
        1697636100000,
        1697636400000,
        1697636700000,
        1697637000000,
        1697637300000,
        1697637600000,
        1697637900000,
        1697638200000,
        1697638500000,
        1697638800000,
        1697639100000,
        1697639400000,
        1697639700000,
        1697640000000,
        1697640300000,
        1697640600000,
        1697640900000,
        1697641200000,
        1697641500000,
        1697641800000,
        1697642100000,
        1697642400000,
        1697642700000,
        1697643000000,
        1697643300000,
        1697643600000,
        1697643900000,
        1697644200000,
        1697644500000,
        1697644800000,
        1697645100000,
        1697645400000,
        1697645700000,
        1697646000000,
        1697646300000,
        1697646600000,
        1697646900000,
        1697647200000,
        1697647500000,
        1697647800000,
        1697648100000,
        1697648400000,
        1697648700000,
        1697649000000,
        1697649300000,
        1697649600000,
        1697649900000,
        1697650200000,
        1697650500000,
        1697650800000,
        1697651100000,
        1697651400000,
        1697651700000,
        1697652000000,
        1697652300000,
        1697652600000,
        1697652900000,
        1697653200000,
        1697653500000,
        1697653800000,
        1697654100000,
        1697654400000,
        1697654700000,
        1697655000000,
        1697655300000,
        1697655600000,
        1697655900000,
        1697656200000,
        1697656500000,
        1697656800000,
        1697657100000,
        1697657400000,
        1697657700000,
        1697658000000,
        1697658300000,
        1697658600000,
        1697658900000,
        1697659200000,
        1697659500000,
        1697659800000,
        1697660100000,
        1697660400000,
        1697660700000,
        1697661000000,
        1697661300000,
        1697661600000,
        1697661900000,
        1697662200000,
        1697662500000,
        1697662800000,
        1697663100000,
        1697663400000,
        1697663700000,
        1697664000000,
        1697664300000,
        1697664600000,
        1697664900000,
        1697665200000,
        1697665500000,
        1697665800000,
        1697666100000,
        1697666400000,
        1697666700000,
        1697667000000,
        1697667300000,
        1697667600000,
        1697667900000,
        1697668200000,
        1697668500000,
        1697668800000,
        1697669100000,
        1697669400000,
        1697669700000,
        1697670000000,
        1697670300000,
        1697670600000,
        1697670900000,
        1697671200000,
        1697671500000,
        1697671800000,
        1697672100000,
        1697672400000,
        1697672700000,
        1697673000000,
        1697673300000,
        1697673600000,
        1697673900000,
        1697674200000,
        1697674500000,
        1697674800000,
        1697675100000,
        1697675400000,
        1697675700000,
        1697676000000,
        1697676300000,
        1697676600000,
        1697676900000,
        1697677200000,
        1697677500000,
        1697677500000,
        1697677800000,
        1697678100000,
        1697678400000,
        1697678700000,
        1697679000000,
        1697679300000,
        1697679600000,
        1697679900000,
        1697680200000,
        1697680500000,
        1697680800000,
        1697681100000,
        1697681400000,
        1697681700000,
        1697682000000,
        1697682300000,
        1697682600000,
        1697682900000,
        1697683200000,
        1697683500000,
        1697683800000,
        1697684100000,
        1697684400000,
        1697684700000,
        1697685000000,
        1697685300000,
        1697685600000,
        1697685900000,
        1697686200000,
        1697686500000,
        1697686800000,
        1697687100000,
        1697687400000,
        1697687700000,
        1697688000000,
        1697688300000,
        1697688600000,
        1697688900000,
        1697689200000,
        1697689500000,
        1697689800000,
        1697690100000,
        1697690400000,
        1697690700000,
        1697691000000,
        1697691300000,
        1697691600000,
        1697691900000,
        1697692200000,
        1697692500000,
        1697692800000,
        1697693100000,
        1697693400000,
        1697693700000,
        1697694000000,
        1697694300000,
        1697694600000,
        1697694900000,
        1697695200000,
        1697695500000,
        1697695800000,
        1697696100000,
        1697696400000,
        1697696700000,
        1697697000000,
        1697697300000,
        1697697600000,
        1697697900000,
        1697698200000,
        1697698500000,
        1697698800000,
        1697699100000,
        1697699400000,
        1697699700000,
        1697700000000,
        1697700300000,
        1697700600000,
        1697700900000,
        1697701200000,
        1697701500000,
        1697701800000,
        1697702100000,
        1697702400000,
        1697702700000,
        1697703000000,
        1697703300000,
        1697703600000,
        1697703900000,
        1697704200000
        };
    }
}
